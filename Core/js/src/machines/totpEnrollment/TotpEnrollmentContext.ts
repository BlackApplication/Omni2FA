/** Snapshot context of <see cref="createTotpEnrollmentMachine"/>. */
export interface TotpEnrollmentContext {
    enrollmentId: string | null;
    otpAuthUri: string | null;
    secret: string | null;
    methodId: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
