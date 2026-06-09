import type { EmailEnrollmentContext } from '@omni2fa/core';

export type EmailEnrollmentStatus = 'idle' | 'starting' | 'awaitingCode' | 'resending' | 'confirming' | 'enrolled' | 'failed';

/** Shape returned by <c>useEmailEnrollment</c>. */
export interface IUseEmailEnrollmentResult {
    /** Current state node name. */
    status: EmailEnrollmentStatus;
    /** Snapshot context — fields populated depending on <c>status</c>. */
    context: EmailEnrollmentContext;
    /** Kick off enrollment — emails a code to the destination address. Pass <paramref name="email"/>
     *  only when the server uses <c>EmailEnrollmentAddressSource.HostSupplied</c>; under the default
     *  <c>ClaimOnly</c> source the address comes from the session and the argument can be omitted. */
    start: (email?: string) => void;
    /** Submit the emailed code (and optional display name). */
    submit: (code: string, name?: string) => void;
    /** Re-send the code, subject to the resend cooldown. */
    resend: () => void;
    /** Cancel/restart — wipes context and returns to <c>idle</c>. */
    reset: () => void;
}
