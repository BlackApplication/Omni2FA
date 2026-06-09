import { useSelector } from '@xstate/react';
import type { EmailEnrollmentActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseEmailEnrollmentResult } from '../Interfaces/IUseEmailEnrollmentResult';

/** Subscribe to the Email enrollment actor. Returns current status, context, and named action proxies. */
export function useEmailEnrollment(): IUseEmailEnrollmentResult {
    const omni = useOmni2Fa();
    const actor: EmailEnrollmentActor = omni.emailEnrollment;

    const status = useSelector(actor, (snap) => snap.value);
    const context = useSelector(actor, (snap) => snap.context);

    return {
        status,
        context,
        start: (email?: string) => actor.send({ type: 'start', email }),
        submit: (code, name) => actor.send({ type: 'submit', code, name }),
        resend: () => actor.send({ type: 'resend' }),
        reset: () => actor.send({ type: 'reset' }),
    };
}
