/** Events accepted by <see cref="createTotpEnrollmentMachine"/>. */
export type TotpEnrollmentEvent =
    | { type: 'start' }
    | { type: 'submit'; code: string; name?: string | undefined }
    | { type: 'reset' };
