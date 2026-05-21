import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import type { TotpEnrollmentContext } from './TotpEnrollmentContext';
import type { TotpEnrollmentEvent } from './TotpEnrollmentEvent';

const initialContext: TotpEnrollmentContext = {
    enrollmentId: null,
    otpAuthUri: null,
    secret: null,
    methodId: null,
    errorCode: null,
    errorMessage: null,
};

export function createTotpEnrollmentMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as TotpEnrollmentContext,
            events: {} as TotpEnrollmentEvent,
        },
        actors: {
            startEnrollment: fromPromise(async () => {
                const result = await client.startTotpEnrollment();
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            confirmEnrollment: fromPromise(async ({ input }: { input: { enrollmentId: string; code: string; name: string | null } }) => {
                const result = await client.confirmTotpEnrollment({
                    enrollmentId: input.enrollmentId,
                    code: input.code,
                    name: input.name,
                });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
        },
    }).createMachine({
        id: 'totpEnrollment',
        initial: 'idle',
        context: initialContext,
        states: {
            idle: {
                on: {
                    start: { target: 'starting' },
                },
            },
            starting: {
                invoke: {
                    src: 'startEnrollment',
                    onDone: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => {
                            context.enrollmentId = event.output.enrollmentId;
                            context.otpAuthUri = event.output.otpAuthUri;
                            context.secret = event.output.secret;
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
            awaitingCode: {
                on: {
                    submit: { target: 'confirming' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            confirming: {
                invoke: {
                    src: 'confirmEnrollment',
                    input: ({ context, event }) => {
                        if (event.type !== 'submit') throw new Error('confirming requires submit event');
                        if (!context.enrollmentId) throw new Error('no enrollmentId');
                        return { enrollmentId: context.enrollmentId, code: event.code, name: event.name ?? null };
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
                        target: 'awaitingCode',
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
                    start: { target: 'starting' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
        },
    });
}

function assignInitial({ context }: { context: TotpEnrollmentContext }) {
    context.enrollmentId = null;
    context.otpAuthUri = null;
    context.secret = null;
    context.methodId = null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: TotpEnrollmentContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type TotpEnrollmentMachine = ReturnType<typeof createTotpEnrollmentMachine>;
export type TotpEnrollmentActor = ActorRefFrom<TotpEnrollmentMachine>;
