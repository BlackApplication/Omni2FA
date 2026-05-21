import type { IOmni2FaClient } from '../client/Interfaces/IOmni2FaClient';
import type { ChallengeActor } from '../machines/challenge/challengeMachine';
import type { MethodsActor } from '../machines/methods/methodsMachine';
import type { TotpEnrollmentActor } from '../machines/totpEnrollment/totpEnrollmentMachine';

/** Assembled Omni2FA core — client plus three running xstate actors and a teardown hook. */
export interface IOmni2Fa {
    client: IOmni2FaClient;
    totpEnrollment: TotpEnrollmentActor;
    challenge: ChallengeActor;
    methods: MethodsActor;
    /** Stops all internal actors. Call on app teardown or when switching users. */
    dispose(): void;
}
