import type { ChallengeContext } from '@omni2fa/core';

export type ChallengeStatus = 'idle' | 'starting' | 'awaitingCode' | 'verifying' | 'verified' | 'failed';

/** Shape returned by <c>useChallenge</c>. */
export interface IUseChallengeResult {
    status: ChallengeStatus;
    context: ChallengeContext;
    /** Begin verification of the chosen method. */
    pick: (methodId: string) => void;
    /** Submit the code/assertion for verification. */
    submit: (code: string) => void;
    /** Cancel/restart — wipes context and returns to <c>idle</c>. */
    reset: () => void;
}
