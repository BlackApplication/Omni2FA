import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import type { EmailEnrollmentContext } from './EmailEnrollmentContext';
import type { EmailEnrollmentEvent } from './EmailEnrollmentEvent';

const initialContext: EmailEnrollmentContext = {
    enrollmentId: null,
    email: null,
    expiresAt: null,
    resendAvailableAt: null,
    methodId: null,
    errorCode: null,
    errorMessage: null,
};

export function createEmailEnrollmentMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as EmailEnrollmentContext,
            events: {} as EmailEnrollmentEvent,
        },
        actors: {
            startEnrollment: fromPromise(async ({ input }: { input: { email: string } }) => {
                const result = await client.startEmailEnrollment({ email: input.email });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            resendEnrollment: fromPromise(async ({ input }: { input: { enrollmentId: string } }) => {
                const result = await client.resendEmailEnrollment({ enrollmentId: input.enrollmentId });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            confirmEnrollment: fromPromise(async ({ input }: { input: { enrollmentId: string; code: string; name: string | null } }) => {
                const result = await client.confirmEmailEnrollment({
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
        id: 'emailEnrollment',
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
                        context.email = event.email;
                    }
                },
                invoke: {
                    src: 'startEnrollment',
                    input: ({ context }) => {
                        if (!context.email) throw new Error('no email');
                        return { email: context.email };
                    },
                    onDone: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => assignStartOutput(context, event.output),
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
                    resend: { target: 'resending' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            resending: {
                invoke: {
                    src: 'resendEnrollment',
                    input: ({ context }) => {
                        if (!context.enrollmentId) throw new Error('no enrollmentId');
                        return { enrollmentId: context.enrollmentId };
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

function assignStartOutput(context: EmailEnrollmentContext, output: { enrollmentId: string; expiresAt: string; resendAvailableAt: string }) {
    context.enrollmentId = output.enrollmentId;
    context.expiresAt = output.expiresAt;
    context.resendAvailableAt = output.resendAvailableAt;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignInitial({ context }: { context: EmailEnrollmentContext }) {
    context.enrollmentId = null;
    context.email = null;
    context.expiresAt = null;
    context.resendAvailableAt = null;
    context.methodId = null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: EmailEnrollmentContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type EmailEnrollmentMachine = ReturnType<typeof createEmailEnrollmentMachine>;
export type EmailEnrollmentActor = ActorRefFrom<EmailEnrollmentMachine>;
