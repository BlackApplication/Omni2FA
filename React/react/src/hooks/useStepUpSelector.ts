import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { StepUpActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the step-up snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useStepUpSelector<T>(selector: (snap: SnapshotFrom<StepUpActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.stepUp, selector);
}
