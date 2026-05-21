import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { ChallengeActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the challenge snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useChallengeSelector<T>(selector: (snap: SnapshotFrom<ChallengeActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.challenge, selector);
}
