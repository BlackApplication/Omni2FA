import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { TotpEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the TOTP enrollment snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useTotpEnrollmentSelector<T>(selector: (snap: SnapshotFrom<TotpEnrollmentActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.totpEnrollment, selector);
}
