/** Events accepted by <see cref="createMethodsMachine"/>. */
export type MethodsEvent = { type: 'load' } | { type: 'remove'; methodId: string } | { type: 'reset' };
