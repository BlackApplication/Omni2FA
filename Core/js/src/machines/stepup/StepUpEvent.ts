/** Events accepted by {@link createStepUpMachine}. */
export type StepUpEvent =
    | { type: 'pick'; methodId: string }
    | { type: 'submit'; code: string }
    | { type: 'resend' }
    | { type: 'reset' };
