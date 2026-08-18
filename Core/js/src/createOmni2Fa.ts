import { createActor } from 'xstate';
import { Omni2FaClient } from './client/Omni2FaClient';
import type { Omni2FaClientConfig } from './client/Omni2FaClientConfig';
import type { IOmni2Fa } from './Interfaces/IOmni2Fa';
import type { IOmni2FaClient } from './client/Interfaces/IOmni2FaClient';
import { createDefaultStorage } from './storage/createDefaultStorage';
import { storageKey } from './storage/storageKey';
import { createTotpEnrollmentMachine } from './machines/totpEnrollment/totpEnrollmentMachine';
import { createEmailEnrollmentMachine } from './machines/emailEnrollment/emailEnrollmentMachine';
import { createWebAuthnEnrollmentMachine } from './machines/webauthnEnrollment/webauthnEnrollmentMachine';
import { createChallengeMachine, type ChallengeActor } from './machines/challenge/challengeMachine';
import { ChallengeResumeStore } from './machines/challenge/ChallengeResumeStore';
import { createStepUpMachine } from './machines/stepup/stepUpMachine';
import { createMethodsMachine } from './machines/methods/methodsMachine';

/**
 * One-call assembly of the Omni2FA core: client + actors for enrollment, login challenge, step-up and the
 * methods list. Actors are started and ready to receive events. Pass the returned instance to your UI adapter.
 */
export function createOmni2Fa(config: Omni2FaClientConfig): IOmni2Fa {
    const storage = config.storage ?? createDefaultStorage();
    const client = new Omni2FaClient({ ...config, storage });
    const resumeStore = new ChallengeResumeStore(storage, config.challengeStorageKey ?? storageKey(config.namespace, 'challenge'));

    const totpEnrollment = createActor(createTotpEnrollmentMachine(client));
    const emailEnrollment = createActor(createEmailEnrollmentMachine(client));
    const webauthnEnrollment = createActor(createWebAuthnEnrollmentMachine(client));
    const challenge = createActor(createChallengeMachine(client));
    const stepUp = createActor(createStepUpMachine(client));
    const methods = createActor(createMethodsMachine(client));

    totpEnrollment.start();
    emailEnrollment.start();
    webauthnEnrollment.start();
    challenge.start();
    stepUp.start();
    methods.start();

    resumeChallenge(challenge, client, resumeStore);
    const persistence = persistChallenge(challenge, resumeStore);

    return {
        client,
        totpEnrollment,
        emailEnrollment,
        webauthnEnrollment,
        challenge,
        stepUp,
        methods,
        dispose() {
            persistence.unsubscribe();
            totpEnrollment.stop();
            emailEnrollment.stop();
            webauthnEnrollment.stop();
            challenge.stop();
            stepUp.stop();
            methods.stop();
        },
    };
}

/**
 * Put a reloaded tab back on the code screen. Without the pre-auth token the screen could not verify
 * anything, so the saved challenge goes with it.
 */
function resumeChallenge(challenge: ChallengeActor, client: IOmni2FaClient, resumeStore: ChallengeResumeStore) {
    if (client.getPreAuthToken() === null) {
        resumeStore.clear();
        return;
    }
    const saved = resumeStore.read();
    if (saved !== null) {
        challenge.send({ type: 'resume', state: saved });
    }
}

function persistChallenge(challenge: ChallengeActor, resumeStore: ChallengeResumeStore) {
    return challenge.subscribe((snapshot) => {
        if (snapshot.value === 'awaitingCode') {
            const { methodId, methodType, expiresAt, resendAvailableAt } = snapshot.context;
            if (methodId !== null && methodType !== null) {
                resumeStore.write({ methodId, methodType, expiresAt, resendAvailableAt });
            }
            return;
        }
        if (snapshot.value === 'idle' || snapshot.value === 'verified' || snapshot.value === 'failed') {
            resumeStore.clear();
        }
    });
}
