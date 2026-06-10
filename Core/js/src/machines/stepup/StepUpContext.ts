import type { TwoFactorMethodType } from '../../types/dtos';

/** Snapshot context of {@link createStepUpMachine}. */
export interface StepUpContext {
    methodId: string | null;
    methodType: TwoFactorMethodType | null;
    /** Single-use step-up token, set at `verified`. Attach it in the step-up header when retrying the protected request. */
    stepUpToken: string | null;
    /** UTC ISO instant an Email step-up code stops validating. Null for TOTP. */
    expiresAt: string | null;
    /** UTC ISO instant an Email step-up code may be re-sent. Null for TOTP. */
    resendAvailableAt: string | null;
    /** WebAuthn assertion options JSON, set while a passkey step-up is in progress. */
    optionsJson: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
