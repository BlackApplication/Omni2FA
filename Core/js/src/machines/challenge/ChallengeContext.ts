import type { TwoFactorMethodType } from '../../types/dtos/TwoFactorMethodType';

/** Snapshot context of <see cref="createChallengeMachine"/>. */
export interface ChallengeContext {
    methodId: string | null;
    methodType: TwoFactorMethodType | null;
    userId: string | null;
    errorCode: string | null;
    errorMessage: string | null;
}
