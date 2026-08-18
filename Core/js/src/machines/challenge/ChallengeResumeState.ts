import type { TwoFactorMethodType } from '../../types/dtos';

/**
 * What a reloaded tab needs to land back on the code screen without starting a new challenge —
 * a new one would send a fresh code and invalidate the one the user already has.
 */
export interface ChallengeResumeState {
    methodId: string;
    methodType: TwoFactorMethodType;
    expiresAt: string | null;
    resendAvailableAt: string | null;
}
