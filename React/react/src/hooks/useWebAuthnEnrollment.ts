import { useSelector } from '@xstate/react';
import type { WebAuthnEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseWebAuthnEnrollmentResult } from '../Interfaces/IUseWebAuthnEnrollmentResult';

/** Subscribe to the WebAuthn enrollment actor. Returns current status, context, and named action proxies. */
export function useWebAuthnEnrollment(): IUseWebAuthnEnrollmentResult {
    const omni = useOmni2Fa();
    const actor: WebAuthnEnrollmentActor = omni.webauthnEnrollment;

    const status = useSelector(actor, (snap) => snap.value);
    const context = useSelector(actor, (snap) => snap.context);

    return {
        status,
        context,
        start: (name) => actor.send({ type: 'start', name }),
        retry: () => actor.send({ type: 'retry' }),
        reset: () => actor.send({ type: 'reset' }),
    };
}
