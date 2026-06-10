import { useEffect } from 'react';
import { useStepUp } from '@omni2fa/react';
import { omni } from '../omni2fa';
import { StepUpDialog } from './StepUpDialog';

/**
 * Registers the step-up handler for the library's **own** sensitive calls (remove method, regenerate
 * recovery codes, enroll start) — those that `MapOmni2Fa` mounts and the host can't decorate. When the
 * server enables a `RequireTwoFactorTo*` flag, such a call returns 403 STEP_UP_REQUIRED; the omni client
 * then invokes `confirmTwoFactor` (opening this dialog) and retries with the token. Mount once, app-wide.
 */
export function StepUpModalHost() {
    const stepUp = useStepUp();

    useEffect(() => {
        omni.client.setStepUpHandler(stepUp.confirmTwoFactor);
        return () => omni.client.setStepUpHandler(null);
    }, [stepUp.confirmTwoFactor]);

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
