/** Snapshot context of <see cref="createEmailEnrollmentMachine"/>. */
export interface EmailEnrollmentContext {
    enrollmentId: string | null;
    email: string | null;
    /** UTC ISO instant the emailed code stops validating. */
    expiresAt: string | null;
    /** UTC ISO instant a resend becomes permitted. */
    resendAvailableAt: string | null;
    methodId: string | null;
    /** Recovery codes returned if this was the user's first method — shown once. Null otherwise. */
    recoveryCodes: string[] | null;
    errorCode: string | null;
    errorMessage: string | null;
}
