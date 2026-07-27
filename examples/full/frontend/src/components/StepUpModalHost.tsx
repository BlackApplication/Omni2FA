import { useStepUp } from '@omni2fa/react';
import { StepUpDialog } from './StepUpDialog';

/**
 * Hosts the prompt for the library's **own** sensitive calls (remove method, regenerate recovery codes,
 * enroll start) — those that `MapOmni2Fa` mounts and the host can't decorate. When the server enables a
 * `RequireTwoFactorTo*` flag, such a call returns 403 STEP_UP_REQUIRED; the omni client invokes the handler
 * `useStepUp` registered for us (opening this dialog) and retries with the token. Mount once, app-wide.
 */
export function StepUpModalHost() {
    const stepUp = useStepUp();

    return (
        <StepUpDialog
            active={stepUp.active}
            methods={stepUp.methods}
            status={stepUp.status}
            context={stepUp.context}
            pick={stepUp.pick}
            submit={stepUp.submit}
            resend={stepUp.resend}
            cancel={stepUp.cancel}
        />
    );
}
