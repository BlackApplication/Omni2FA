import type { TwoFactorMethodType } from '../../types/dtos';

/** Snapshot context of <see cref="createChallengeMachine"/>. */
export interface ChallengeContext {
    methodId: string | null;
    methodType: TwoFactorMethodType | null;
    userId: string | null;
    /** UTC ISO instant an Email login code stops validating. Null for TOTP. */
    expiresAt: string | null;
    /** UTC ISO instant an Email login code may be re-sent. Null for TOTP. */
    resendAvailableAt: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
