import type { TotpEnrollmentContext } from '@omni2fa/core';

export type TotpEnrollmentStatus = 'idle' | 'starting' | 'awaitingCode' | 'confirming' | 'enrolled' | 'failed';

/** Shape returned by <c>useTotpEnrollment</c>. */
export interface IUseTotpEnrollmentResult {
    /** Current state node name. */
    status: TotpEnrollmentStatus;
    /** Snapshot context — fields populated depending on <c>status</c>. */
    context: TotpEnrollmentContext;
    /** Kick off enrollment — TOTP secret + QR URI become available in <c>context</c>. */
    start: () => void;
    /** Submit the first authenticator code (and optional display name). */
    submit: (code: string, name?: string) => void;
    /** Cancel/restart — wipes context and returns to <c>idle</c>. */
    reset: () => void;
}
