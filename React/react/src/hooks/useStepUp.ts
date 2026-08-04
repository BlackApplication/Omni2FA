import { useCallback, useEffect, useRef, useState } from 'react';
import { useSelector } from '@xstate/react';
import type { StepUpActor, TwoFactorMethodDto } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseStepUpOptions } from '../Interfaces/IUseStepUpOptions';
import type { IUseStepUpResult } from '../Interfaces/IUseStepUpResult';

/**
 * Step-up (action confirmation) hook. Call <c>confirmTwoFactor(methods)</c> when the server answers
 * <c>403 STEP_UP_REQUIRED</c>: it shows the 2FA prompt (drive it with <c>active</c>/<c>methods</c>/<c>status</c>
 * and <c>pick</c>/<c>submit</c>) and resolves a single-use step-up token. Attach the token in the
 * <c>X-Omni2FA-StepUp</c> header on your retry — the library stays out of how you make the request.
 * The prompt reuses the same method-picker/code UI as the login challenge.
 *
 * The library's own gated endpoints are wired up for you: while this hook is mounted it registers
 * <c>confirmTwoFactor</c> on the client (opt out with <c>{ handleClientStepUp: false }</c>).
 *
 * When the backend grants a grace window, <c>confirmTwoFactor</c> resolves from the last confirmation —
 * a step-up or the 2FA login itself — while it is still inside that window, instead of prompting again.
 */
export function useStepUp(options?: IUseStepUpOptions): IUseStepUpResult {
    const omni = useOmni2Fa();
    const actor: StepUpActor = omni.stepUp;

    const status = useSelector(actor, (snap) => snap.value);
    const context = useSelector(actor, (snap) => snap.context);

    const [active, setActive] = useState(false);
    const [methods, setMethods] = useState<TwoFactorMethodDto[]>([]);
    // Resolver for the in-flight confirmTwoFactor() promise, settled when the ceremony verifies or is cancelled.
    const pending = useRef<((token: string | null) => void) | null>(null);

    const settle = useCallback(
        (token: string | null) => {
            if (pending.current) {
                pending.current(token);
                pending.current = null;
            }
            setActive(false);
            actor.send({ type: 'reset' });
        },
        [actor],
    );

    // When the ceremony reaches `verified`, hand the fresh token back to the waiting caller.
    useEffect(() => {
        if (status === 'verified' && context.stepUpToken && pending.current) {
            settle(context.stepUpToken);
        }
    }, [status, context.stepUpToken, settle]);

    const confirmTwoFactor = useCallback(
        (available: TwoFactorMethodDto[]) => {
            // Null unless the backend granted a window, so prompting stays the default.
            const cached = omni.client.peekStepUpToken();
            if (cached) {
                return Promise.resolve<string | null>(cached);
            }
            return new Promise<string | null>((resolve) => {
                setMethods(available);
                actor.send({ type: 'reset' });
                setActive(true);
                pending.current = resolve;
            });
        },
        [actor, omni],
    );

    const handleClientStepUp = options?.handleClientStepUp ?? true;
    useEffect(() => {
        if (!handleClientStepUp) {
            return;
        }
        omni.client.setStepUpHandler(confirmTwoFactor);
        return () => omni.client.setStepUpHandler(null);
    }, [handleClientStepUp, omni, confirmTwoFactor]);

    return {
        confirmTwoFactor,
        active,
        methods,
        status,
        context,
        pick: (methodId) => actor.send({ type: 'pick', methodId }),
        submit: (code) => actor.send({ type: 'submit', code }),
        resend: () => actor.send({ type: 'resend' }),
        cancel: () => settle(null),
    };
}
