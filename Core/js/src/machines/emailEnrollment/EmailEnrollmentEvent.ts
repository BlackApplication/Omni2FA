/** Events accepted by <see cref="createEmailEnrollmentMachine"/>. */
export type EmailEnrollmentEvent =
    | { type: 'start'; email: string }
    | { type: 'submit'; code: string; name?: string | undefined }
    | { type: 'resend' }
    | { type: 'reset' };
