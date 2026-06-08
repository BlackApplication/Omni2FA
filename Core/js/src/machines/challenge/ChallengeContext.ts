import type { TwoFactorMethodType } from '../../types/dtos';

/** Snapshot context of <see cref="createChallengeMachine"/>. */
export interface ChallengeContext {
    methodId: string | null;
    methodType: TwoFactorMethodType | null;
    userId: string | null;
    /** Verified-handoff token, set at `verified`. Send this to the host's finalize endpoint, not the pre-auth token. */
    verifiedToken: string | null;
    /** UTC ISO instant an Email login code stops validating. Null for TOTP. */
    expiresAt: string | null;
    /** UTC ISO instant an Email login code may be re-sent. Null for TOTP. */
    resendAvailableAt: string | null;
    /** WebAuthn assertion options JSON, set while a passkey login is in progress. */
    optionsJson: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
