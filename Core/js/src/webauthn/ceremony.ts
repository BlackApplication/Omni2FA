import { base64urlToBuffer, bufferToBase64url } from './base64url';

/**
 * Browser-side WebAuthn marshaling. Parses the server's options JSON (Fido2NetLib format — binary
 * fields base64url-encoded) into the `ArrayBuffer` shape the browser API needs, runs the ceremony,
 * and serializes the credential back to JSON for the server. Browser-only.
 */

interface CredentialDescriptorJson {
    type: PublicKeyCredentialType;
    id: string;
    transports?: AuthenticatorTransport[];
}

interface CreationOptionsJson {
    challenge: string;
    user: { id: string; name: string; displayName: string };
    excludeCredentials?: CredentialDescriptorJson[];
    [key: string]: unknown;
}

interface RequestOptionsJson {
    challenge: string;
    allowCredentials?: CredentialDescriptorJson[];
    [key: string]: unknown;
}

function mapDescriptors(list: CredentialDescriptorJson[] | undefined): PublicKeyCredentialDescriptor[] {
    return (list ?? []).map((c) => ({ ...c, id: base64urlToBuffer(c.id) }));
}

/** Run a registration ceremony and return the attestation JSON for `/enroll/webauthn/confirm`. */
export async function startRegistration(optionsJson: string): Promise<string> {
    const parsed = JSON.parse(optionsJson) as CreationOptionsJson;
    const publicKey = {
        ...parsed,
        challenge: base64urlToBuffer(parsed.challenge),
        user: { ...parsed.user, id: base64urlToBuffer(parsed.user.id) },
        excludeCredentials: mapDescriptors(parsed.excludeCredentials),
        // Cast through unknown: the spread carries Fido2's index-signature fields the DOM type omits.
    } as unknown as PublicKeyCredentialCreationOptions;

    const credential = (await navigator.credentials.create({ publicKey })) as PublicKeyCredential | null;
    if (credential === null) {
        throw new Error('WebAuthn registration produced no credential.');
    }
    const response = credential.response as AuthenticatorAttestationResponse;
    return JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,
        extensions: credential.getClientExtensionResults(),
        response: {
            attestationObject: bufferToBase64url(response.attestationObject),
            clientDataJSON: bufferToBase64url(response.clientDataJSON),
        },
    });
}

/** Run an assertion ceremony and return the assertion JSON for `/challenge/verify`. */
export async function startAuthentication(optionsJson: string): Promise<string> {
    const parsed = JSON.parse(optionsJson) as RequestOptionsJson;
    const publicKey = {
        ...parsed,
        challenge: base64urlToBuffer(parsed.challenge),
        allowCredentials: mapDescriptors(parsed.allowCredentials),
    } as unknown as PublicKeyCredentialRequestOptions;

    const credential = (await navigator.credentials.get({ publicKey })) as PublicKeyCredential | null;
    if (credential === null) {
        throw new Error('WebAuthn authentication produced no credential.');
    }
    const response = credential.response as AuthenticatorAssertionResponse;
    return JSON.stringify({
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,
        extensions: credential.getClientExtensionResults(),
        response: {
            authenticatorData: bufferToBase64url(response.authenticatorData),
            clientDataJSON: bufferToBase64url(response.clientDataJSON),
            signature: bufferToBase64url(response.signature),
            userHandle: response.userHandle ? bufferToBase64url(response.userHandle) : null,
        },
    });
}
