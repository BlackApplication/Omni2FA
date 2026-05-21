import { fromPromise, setup, type ActorRefFrom } from 'xstate';
import type { IOmni2FaClient } from '../../client/Interfaces/IOmni2FaClient';
import { Omni2FaApiError } from '../../errors/Omni2FaApiError';
import type { MethodsContext } from './MethodsContext';
import type { MethodsEvent } from './MethodsEvent';

const initialContext: MethodsContext = {
    items: [],
    errorCode: null,
    errorMessage: null,
};

export function createMethodsMachine(client: IOmni2FaClient) {
    return setup({
        types: {
            context: {} as MethodsContext,
            events: {} as MethodsEvent,
        },
        actors: {
            load: fromPromise(async () => {
                const result = await client.listMethods();
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return result.value;
            }),
            remove: fromPromise(async ({ input }: { input: { methodId: string } }) => {
                const result = await client.removeMethod(input.methodId);
                if (!result.ok) {
                    throw new Omni2FaApiError(result.code, result.message, result.httpStatus, result.details ?? null);
                }
                return input.methodId;
            }),
        },
    }).createMachine({
        id: 'methods',
        initial: 'idle',
        context: initialContext,
        states: {
            idle: {
                on: {
                    load: { target: 'loading' },
                },
            },
            loading: {
                invoke: {
                    src: 'load',
                    onDone: {
                        target: 'ready',
                        actions: ({ context, event }) => {
                            context.items = event.output;
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
            ready: {
                on: {
                    load: { target: 'loading' },
                    remove: { target: 'removing' },
                },
            },
            removing: {
                invoke: {
                    src: 'remove',
                    input: ({ event }) => {
                        if (event.type !== 'remove') throw new Error('removing requires remove event');
                        return { methodId: event.methodId };
                    },
                    onDone: {
                        target: 'ready',
                        actions: ({ context, event }) => {
                            const removedId = event.output;
                            context.items = context.items.filter((m) => m.id !== removedId);
                            context.errorCode = null;
                            context.errorMessage = null;
                        },
                    },
                    onError: {
                        target: 'ready',
                        actions: ({ context, event }) => assignApiError(context, event.error),
                    },
                },
            },
            failed: {
                on: {
                    load: { target: 'loading' },
                    reset: { target: 'idle', actions: assignInitial },
                },
            },
        },
    });
}

function assignInitial({ context }: { context: MethodsContext }) {
    context.items = [];
    context.errorCode = null;
    context.errorMessage = null;
}

function assignApiError(context: MethodsContext, error: unknown) {
    if (error instanceof Omni2FaApiError) {
        context.errorCode = error.code;
        context.errorMessage = error.message;
    } else {
        context.errorCode = 'UNKNOWN';
        context.errorMessage = error instanceof Error ? error.message : null;
    }
}

export type MethodsMachine = ReturnType<typeof createMethodsMachine>;
export type MethodsActor = ActorRefFrom<MethodsMachine>;
