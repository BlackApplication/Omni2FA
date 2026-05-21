import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import type { ChallengeContext } from './ChallengeContext';
import type { ChallengeEvent } from './ChallengeEvent';

const initialContext: ChallengeContext = {
    methodId: null,
    methodType: null,
    userId: null,
    errorCode: null,
    errorMessage: null,
};

export function createChallengeMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as ChallengeContext,
            events: {} as ChallengeEvent,
        },
        actors: {
            startChallenge: fromPromise(async ({ input }: { input: { methodId: string } }) => {
                const result = await client.startChallenge({ methodId: input.methodId });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            verifyChallenge: fromPromise(async ({ input }: { input: { methodId: string; code: string } }) => {
                const result = await client.verifyChallenge({ methodId: input.methodId, code: input.code });
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
        },
    }).createMachine({
        id: 'challenge',
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
                    src: 'startChallenge',
                    input: ({ context }) => {
                        if (!context.methodId) throw new Error('no methodId');
                        return { methodId: context.methodId };
                    },
                    onDone: {
                        target: 'awaitingCode',
                        actions: ({ context, event }) => {
                            context.methodType = event.output.type;
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
                    submit: { target: 'verifying' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
            verifying: {
                invoke: {
                    src: 'verifyChallenge',
                    input: ({ context, event }) => {
                        if (event.type !== 'submit') throw new Error('verifying requires submit event');
                        if (!context.methodId) throw new Error('no methodId');
                        return { methodId: context.methodId, code: event.code };
                    },
                    onDone: {
                        target: 'verified',
                        actions: ({ context, event }) => {
                            context.userId = event.output.userId;
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

function assignInitial({ context }: { context: ChallengeContext }) {
    context.methodId = null;
    context.methodType = null;
    context.userId = null;
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: ChallengeContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type ChallengeMachine = ReturnType<typeof createChallengeMachine>;
export type ChallengeActor = ActorRefFrom<ChallengeMachine>;
