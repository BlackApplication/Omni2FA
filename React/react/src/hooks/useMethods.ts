import { useEffect } from 'react';
import { useSelector } from '@xstate/react';
import type { MethodsActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseMethodsOptions } from '../Interfaces/IUseMethodsOptions';
import type { IUseMethodsResult } from '../Interfaces/IUseMethodsResult';

/** Subscribe to the methods-list actor. Auto-loads on mount unless <c>autoLoad: false</c> is passed. */
export function useMethods(options: IUseMethodsOptions = {}): IUseMethodsResult {
    const { autoLoad = true } = options;
    const omni = useOmni2Fa();
    const actor: MethodsActor = omni.methods;

    const status = useSelector(actor, (snap) => snap.value);
    const items = useSelector(actor, (snap) => snap.context.items);
    const errorCode = useSelector(actor, (snap) => snap.context.errorCode);
    const errorMessage = useSelector(actor, (snap) => snap.context.errorMessage);

    useEffect(() => {
        if (autoLoad && actor.getSnapshot().value === 'idle') {
            actor.send({ type: 'load' });
        }
    }, [autoLoad, actor]);

    return {
        status,
        items,
        errorCode,
        errorMessage,
        load: () => actor.send({ type: 'load' }),
        remove: (methodId) => actor.send({ type: 'remove', methodId }),
        reset: () => actor.send({ type: 'reset' }),
    };
}
