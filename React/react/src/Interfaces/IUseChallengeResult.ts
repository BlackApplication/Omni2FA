import type { ChallengeContext, ChallengeResumeState } from '@omni2fa/core';

export type ChallengeStatus = 'idle' | 'starting' | 'awaitingCode' | 'resending' | 'asserting' | 'verifying' | 'verifyingRecovery' | 'verified' | 'failed';

/** Shape returned by <c>useChallenge</c>. */
export interface IUseChallengeResult {
    status: ChallengeStatus;
    context: ChallengeContext;
    /** Begin verification of the chosen method. */
    pick: (methodId: string) => void;
    /** Submit the code/assertion for verification. */
    submit: (code: string) => void;
    /** Re-send the login OTP (Email methods only), subject to the resend cooldown. */
    resend: () => void;
    /** Complete login with a one-time recovery code instead of a method. */
    useRecoveryCode: (code: string) => void;
    /**
     * Return to the code screen for a challenge that is already running, without starting a new one.
     * The core does this on its own for a reloaded tab; call it only when the host keeps its own copy
     * of the challenge somewhere the core cannot see.
     */
    resume: (state: ChallengeResumeState) => void;
    /** Cancel/restart — wipes context and returns to <c>idle</c>. */
    reset: () => void;
}
