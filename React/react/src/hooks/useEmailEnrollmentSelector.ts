import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { EmailEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the Email enrollment snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useEmailEnrollmentSelector<T>(selector: (snap: SnapshotFrom<EmailEnrollmentActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.emailEnrollment, selector);
}
