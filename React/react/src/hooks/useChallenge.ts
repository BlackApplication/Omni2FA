import { useSelector } from '@xstate/react';
import type { ChallengeActor } from '@omni2fa/core';
import { useOmni2Fa } from './useOmni2Fa';
import type { IUseChallengeResult } from '../Interfaces/IUseChallengeResult';

/** Subscribe to the login challenge actor. Returns current status, context, and named action proxies. */
export function useChallenge(): IUseChallengeResult {
    const omni = useOmni2Fa();
    const actor: ChallengeActor = omni.challenge;

    const status = useSelector(actor, (snap) => snap.value);
    const context = useSelector(actor, (snap) => snap.context);

    return {
        status,
        context,
        pick: (methodId) => actor.send({ type: 'pick', methodId }),
        submit: (code) => actor.send({ type: 'submit', code }),
        reset: () => actor.send({ type: 'reset' }),
    };
}
