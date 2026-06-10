import type {
    ChallengeResendRequest,
    ChallengeStartRequest,
    ChallengeStartResponse,
    ChallengeVerifyRequest,
    EmailEnrollConfirmRequest,
    EmailEnrollResendRequest,
    EmailEnrollStartRequest,
    EmailEnrollStartResponse,
    MethodCreatedResponse,
    RecoveryCodesResponse,
    RecoveryCodeVerifyRequest,
    StepUpVerifyResponse,
    TotpEnrollConfirmRequest,
    TotpEnrollStartResponse,
    TwoFactorMethodDto,
    VerifySuccessResponse,
    WebAuthnEnrollConfirmRequest,
    WebAuthnEnrollStartResponse,
} from '../../types/dtos';
import type { ClientCall } from './ClientCall';

/** Typed wrapper over the Omni2FA HTTP contract. Adapters consume this — they never call <c>fetch</c> directly. */
export interface IOmni2FaClient {
    listMethods(): Promise<ClientCall<TwoFactorMethodDto[]>>;
    removeMethod(methodId: string): Promise<ClientCall<void>>;

    startTotpEnrollment(): Promise<ClientCall<TotpEnrollStartResponse>>;
    confirmTotpEnrollment(request: TotpEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>>;

    startEmailEnrollment(request: EmailEnrollStartRequest): Promise<ClientCall<EmailEnrollStartResponse>>;
    confirmEmailEnrollment(request: EmailEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>>;
    resendEmailEnrollment(request: EmailEnrollResendRequest): Promise<ClientCall<EmailEnrollStartResponse>>;

    startWebAuthnEnrollment(): Promise<ClientCall<WebAuthnEnrollStartResponse>>;
    confirmWebAuthnEnrollment(request: WebAuthnEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>>;

    startChallenge(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>>;
    resendChallenge(request: ChallengeResendRequest): Promise<ClientCall<ChallengeStartResponse>>;
    verifyChallenge(request: ChallengeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>>;
    verifyRecoveryCode(request: RecoveryCodeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>>;

    /** Step-up (action confirmation) for an authenticated user. Mirror the challenge flow; verify yields a single-use step-up token. */
    startStepUp(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>>;
    resendStepUp(request: ChallengeResendRequest): Promise<ClientCall<ChallengeStartResponse>>;
    verifyStepUp(request: ChallengeVerifyRequest): Promise<ClientCall<StepUpVerifyResponse>>;

    regenerateRecoveryCodes(): Promise<ClientCall<RecoveryCodesResponse>>;

    /** Pre-auth token — sent on <c>/challenge/*</c> during the 2FA ceremony. */
    setPreAuthToken(token: string | null): void;
    getPreAuthToken(): string | null;

    /** Host session token — sent on host-session endpoints (<c>/methods</c>, <c>/enroll/*</c>, <c>/recovery-codes/*</c>). */
    setSessionToken(token: string | null): void;
    getSessionToken(): string | null;
}
