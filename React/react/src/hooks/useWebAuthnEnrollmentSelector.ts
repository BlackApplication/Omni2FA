import { useSelector } from '@xstate/react';
import type { SnapshotFrom } from 'xstate';
import type { WebAuthnEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';

/** Subscribe to a derived slice of the WebAuthn enrollment snapshot. Re-renders only when <paramref name="selector"/> output changes. */
export function useWebAuthnEnrollmentSelector<T>(selector: (snap: SnapshotFrom<WebAuthnEnrollmentActor>) => T): T {
    const omni = useOmni2Fa();
    return useSelector(omni.webauthnEnrollment, selector);
}
