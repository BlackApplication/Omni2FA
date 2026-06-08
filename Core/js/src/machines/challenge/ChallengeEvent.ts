/** Events accepted by <see cref="createChallengeMachine"/>. */
export type ChallengeEvent =
    | { type: 'pick'; methodId: string }
    | { type: 'submit'; code: string }
    | { type: 'resend' }
    | { type: 'reset' };
