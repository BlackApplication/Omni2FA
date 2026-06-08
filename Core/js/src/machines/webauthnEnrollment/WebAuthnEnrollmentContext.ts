/** Snapshot context of <see cref="createWebAuthnEnrollmentMachine"/>. */
export interface WebAuthnEnrollmentContext {
    enrollmentId: string | null;
    optionsJson: string | null;
    name: string | null;
    methodId: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
