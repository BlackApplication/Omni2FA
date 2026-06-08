/** Snapshot context of <see cref="createTotpEnrollmentMachine"/>. */
export interface TotpEnrollmentContext {
    enrollmentId: string | null;
    otpAuthUri: string | null;
    secret: string | null;
    methodId: string | null;
    /** Recovery codes returned if this was the user's first method — shown once. Null otherwise. */
    recoveryCodes: string[] | null;
    errorCode: string | null;
    errorMessage: string | null;
}
