import { createActor } from 'xstate';
import { Omni2FaClient } from './client/Omni2FaClient';
import type { Omni2FaClientConfig } from './client/Omni2FaClientConfig';
import type { IOmni2Fa } from './Interfaces/IOmni2Fa';
import { createTotpEnrollmentMachine } from './machines/totpEnrollment/totpEnrollmentMachine';
import { createEmailEnrollmentMachine } from './machines/emailEnrollment/emailEnrollmentMachine';
import { createWebAuthnEnrollmentMachine } from './machines/webauthnEnrollment/webauthnEnrollmentMachine';
import { createChallengeMachine } from './machines/challenge/challengeMachine';
import { createMethodsMachine } from './machines/methods/methodsMachine';

/**
 * One-call assembly of the Omni2FA core: client + three actors (TOTP enrollment, login challenge, methods list).
 * Actors are <c>start()</c>ed and ready to receive events. Pass the returned instance to your UI adapter.
 */
export function createOmni2Fa(config: Omni2FaClientConfig): IOmni2Fa {
    const client = new Omni2FaClient(config);

    const totpEnrollment = createActor(createTotpEnrollmentMachine(client));
    const emailEnrollment = createActor(createEmailEnrollmentMachine(client));
    const webauthnEnrollment = createActor(createWebAuthnEnrollmentMachine(client));
    const challenge = createActor(createChallengeMachine(client));
    const methods = createActor(createMethodsMachine(client));

    totpEnrollment.start();
    emailEnrollment.start();
    webauthnEnrollment.start();
    challenge.start();
    methods.start();

    return {
        client,
        totpEnrollment,
        emailEnrollment,
        webauthnEnrollment,
        challenge,
        methods,
        dispose() {
            totpEnrollment.stop();
            emailEnrollment.stop();
            webauthnEnrollment.stop();
            challenge.stop();
            methods.stop();
        },
    };
}
