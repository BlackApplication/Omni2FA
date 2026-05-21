import { useSelector } from '@xstate/react';
import type { TotpEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseTotpEnrollmentResult } from '../Interfaces/IUseTotpEnrollmentResult';

/** Subscribe to the TOTP enrollment actor. Returns current status, context, and named action proxies. */
export function useTotpEnrollment(): IUseTotpEnrollmentResult {
    const omni = useOmni2Fa();
    const actor: TotpEnrollmentActor = omni.totpEnrollment;

    const status = useSelector(actor, (snap) => snap.value);
    const context = useSelector(actor, (snap) => snap.context);

    return {
        status,
        context,
        start: () => actor.send({ type: 'start' }),
        submit: (code, name) => actor.send({ type: 'submit', code, name }),
        reset: () => actor.send({ type: 'reset' }),
    };
}
