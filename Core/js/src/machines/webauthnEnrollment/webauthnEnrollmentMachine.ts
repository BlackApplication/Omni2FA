import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import { startRegistration } from '../../webauthn/ceremony';
import type { WebAuthnEnrollmentContext } from './WebAuthnEnrollmentContext';
import type { WebAuthnEnrollmentEvent } from './WebAuthnEnrollmentEvent';

const initialContext: WebAuthnEnrollmentContext = {
    enrollmentId: null,
    optionsJson: null,
    name: null,
    methodId: null,
    errorCode: null,
    errorMessage: null,
};

export function createWebAuthnEnrollmentMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as WebAuthnEnrollmentContext,
            events: {} as WebAuthnEnrollmentEvent,
        },
        actors: {
            startEnrollment: fromPromise(async () => {
                const result = await client.startWebAuthnEnrollment();
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            registerAndConfirm: fromPromise(async ({ input }: { input: { enrollmentId: string; optionsJson: string; name: string | null } }) => {
                const attestationResponseJson = await startRegistration(input.optionsJson);
                const result = await client.confirmWebAuthnEnrollment({
                    enrollmentId: input.enrollmentId,
                    attestationResponseJson,
                    name: input.name,
                });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
        },
    }).createMachine({
        id: 'webauthnEnrollment',
        initial: 'idle',
        context: initialContext,
        states: {
            idle: {
                on: {
                    start: { target: 'starting' },
                },
            },
            starting: {
                entry: ({ context, event }) => {
                    if (event.type === 'start') {
                        context.name = event.name ?? null;
                    }
                },
                invoke: {
                    src: 'startEnrollment',
                    onDone: {
                        target: 'registering',
                        actions: ({ context, event }) => {
                            context.enrollmentId = event.output.enrollmentId;
                            context.optionsJson = event.output.optionsJson;
                            context.errorCode = null;
                            context.errorMessage = null;
                        },
                    },
                    onError: {
                        target: 'failed',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            registering: {
                invoke: {
                    src: 'registerAndConfirm',
                    input: ({ context }) => {
                        if (!context.enrollmentId || !context.optionsJson) throw new Error('no pending enrollment');
                        return { enrollmentId: context.enrollmentId, optionsJson: context.optionsJson, name: context.name };
                    },
                    onDone: {
                        target: 'enrolled',
                        actions: ({ context, event }) => {
                            context.methodId = event.output.methodId;
                            context.errorCode = null;
                            context.errorMessage = null;
                        },
                    },
                    onError: {
                        target: 'failed',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            enrolled: {
                on: {
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            failed: {
                on: {
                    retry: { target: 'starting' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
        },
    });
}

function assignInitial({ context }: { context: WebAuthnEnrollmentContext }) {
    context.enrollmentId = null;
    context.optionsJson = null;
    context.name = null;
    context.methodId = null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: WebAuthnEnrollmentContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type WebAuthnEnrollmentMachine = ReturnType<typeof createWebAuthnEnrollmentMachine>;
export type WebAuthnEnrollmentActor = ActorRefFrom<WebAuthnEnrollmentMachine>;
