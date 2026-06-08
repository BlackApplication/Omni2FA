/** Events accepted by <see cref="createWebAuthnEnrollmentMachine"/>. */
export type WebAuthnEnrollmentEvent =
    | { type: 'start'; name?: string | undefined }
    | { type: 'retry' }
    | { type: 'reset' };
