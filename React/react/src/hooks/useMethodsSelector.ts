import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { MethodsActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the methods snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useMethodsSelector<T>(selector: (snap: SnapshotFrom<MethodsActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.methods, selector);
}
