import type { StepUpContext, TwoFactorMethodDto } from '@omni2fa/core';

export type StepUpStatus = 'idle' | 'starting' | 'awaitingCode' | 'resending' | 'asserting' | 'verifying' | 'verified' | 'failed';

/** Shape returned by <c>useStepUp</c>. */
export interface IUseStepUpResult {
    /**
     * Show the 2FA prompt and resolve a single-use step-up token, or <c>null</c> if the user cancels.
     * Pass the methods from the server's <c>403 STEP_UP_REQUIRED</c> response. Attach the returned token
     * in the <c>X-Omni2FA-StepUp</c> header (constant <c>STEP_UP_HEADER</c>) when you retry the request —
     * however your app makes it (fetch, axios, cookie or Bearer session; the token rides on top).
     */
    confirmTwoFactor: (methods: TwoFactorMethodDto[]) => Promise<string | null>;
    /** True while a confirmation is in progress — render your 2FA prompt when set. */
    active: boolean;
    /** Methods the user can confirm with, taken from the server's <c>STEP_UP_REQUIRED</c> response. */
    methods: TwoFactorMethodDto[];
    status: StepUpStatus;
    context: StepUpContext;
    /** Begin confirmation with the chosen method. */
    pick: (methodId: string) => void;
    /** Submit the code/assertion for confirmation. */
    submit: (code: string) => void;
    /** Re-send the step-up OTP (Email methods only), subject to the resend cooldown. */
    resend: () => void;
    /** Dismiss the prompt — the pending <c>confirmTwoFactor</c> call resolves with <c>null</c>. */
    cancel: () => void;
}
