import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import { startAuthentication } from '../../webauthn/ceremony';
import type { StepUpContext } from './StepUpContext';
import type { StepUpEvent } from './StepUpEvent';

const initialContext: StepUpContext = {
    methodId: null,
    methodType: null,
    stepUpToken: null,
    expiresAt: null,
    resendAvailableAt: null,
    optionsJson: null,
    errorCode: null,
    errorMessage: null,
};

/**
 * Drives an action-confirmation (step-up) ceremony for an already-authenticated user: pick a method,
 * confirm a code or passkey, end with a single-use `stepUpToken` in context. Mirrors the login
 * challenge machine but hits `/stepup/*` and produces a step-up token instead of a login handoff token.
 * Recovery codes are intentionally not a step-up path — step-up confirms with an enrolled method.
 */
export function createStepUpMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as StepUpContext,
            events: {} as StepUpEvent,
        },
        actors: {
            startStepUp: fromPromise(async ({ input }: { input: { methodId: string } }) => {
                const result = await client.startStepUp({ methodId: input.methodId });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            resendStepUp: fromPromise(async ({ input }: { input: { methodId: string } }) => {
                const result = await client.resendStepUp({ methodId: input.methodId });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            verifyStepUp: fromPromise(async ({ input }: { input: { methodId: string; code: string } }) => {
                const result = await client.verifyStepUp({ methodId: input.methodId, code: input.code });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            assertStepUp: fromPromise(async ({ input }: { input: { methodId: string; optionsJson: string } }) => {
                const assertionResponseJson = await startAuthentication(input.optionsJson);
                const result = await client.verifyStepUp({ methodId: input.methodId, assertionResponseJson });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
        },
    }).createMachine({
        id: 'stepup',
        initial: 'idle',
        context: initialContext,
        states: {
            idle: {
                on: {
                    pick: { target: 'starting' },
                },
            },
            starting: {
                entry: ({ context, event }) => {
                    if (event.type === 'pick') {
                        context.methodId = event.methodId;
                    }
                },
                invoke: {
                    src: 'startStepUp',
                    input: ({ context }) => {
                        if (!context.methodId) throw new Error('no methodId');
                        return { methodId: context.methodId };
                    },
                    onDone: [
                        {
                            guard: ({ event }) => event.output.type === 'WebAuthn',
                            target: 'asserting',
                            actions: ({ context, event }) => assignStartOutput(context, event.output),
                        },
                        {
                            target: 'awaitingCode',
                            actions: ({ context, event }) => assignStartOutput(context, event.output),
                        },
                    ],
                    onError: {
                        target: 'failed',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            asserting: {
                invoke: {
                    src: 'assertStepUp',
                    input: ({ context }) => {
                        if (!context.methodId || !context.optionsJson) throw new Error('no assertion options');
                        return { methodId: context.methodId, optionsJson: context.optionsJson };
                    },
                    onDone: {
                        target: 'verified',
                        actions: ({ context, event }) => assignVerified(context, event.output.stepUpToken),
                    },
                    onError: {
                        target: 'failed',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            awaitingCode: {
                on: {
                    submit: { target: 'verifying' },
                    resend: { target: 'resending' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            resending: {
                invoke: {
                    src: 'resendStepUp',
                    input: ({ context }) => {
                        if (!context.methodId) throw new Error('no methodId');
                        return { methodId: context.methodId };
                    },
                    onDone: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => assignStartOutput(context, event.output),
                    },
                    onError: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            verifying: {
                invoke: {
                    src: 'verifyStepUp',
                    input: ({ context, event }) => {
                        if (event.type !== 'submit') throw new Error('verifying requires submit event');
                        if (!context.methodId) throw new Error('no methodId');
                        return { methodId: context.methodId, code: event.code };
                    },
                    onDone: {
                        target: 'verified',
                        actions: ({ context, event }) => assignVerified(context, event.output.stepUpToken),
                    },
                    onError: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            verified: {
                on: {
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            failed: {
                on: {
                    pick: { target: 'starting' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
        },
    });
}

function assignStartOutput(context: StepUpContext, output: { type: StepUpContext['methodType']; expiresAt?: string | null; resendAvailableAt?: string | null; optionsJson?: string | null }) {
    context.methodType = output.type;
    context.expiresAt = output.expiresAt ?? null;
    context.resendAvailableAt = output.resendAvailableAt ?? null;
    context.optionsJson = output.optionsJson ?? null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignVerified(context: StepUpContext, stepUpToken: string) {
    context.stepUpToken = stepUpToken;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignInitial({ context }: { context: StepUpContext }) {
    context.methodId = null;
    context.methodType = null;
    context.stepUpToken = null;
    context.expiresAt = null;
    context.resendAvailableAt = null;
    context.optionsJson = null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: StepUpContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type StepUpMachine = ReturnType<typeof createStepUpMachine>;
export type StepUpActor = ActorRefFrom<StepUpMachine>;
