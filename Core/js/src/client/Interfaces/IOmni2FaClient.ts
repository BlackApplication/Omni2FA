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

/** Confirms 2FA and resolves a single-use step-up token, or null if the user cancels. Registered via {@link IOmni2FaClient.setStepUpHandler}. */
export type StepUpHandler = (methods: TwoFactorMethodDto[]) => Promise<string | null>;

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

    /**
     * Register a handler that confirms 2FA when one of the library's own sensitive calls (remove method,
     * regenerate recovery codes, enroll start) returns <c>403 STEP_UP_REQUIRED</c> — only when the host
     * enabled the matching server flag. The client invokes it, then retries the call with the step-up
     * header. Pass the React <c>useStepUp().confirmTwoFactor</c> here, or null to clear.
     */
    setStepUpHandler(handler: StepUpHandler | null): void;

    /**
     * A step-up token still inside its grace window, or null — attach it to act without prompting again.
     * Filled from a step-up confirmation or from a 2FA login, and only when the backend grants a window.
     * Call it before showing a prompt.
     */
    peekStepUpToken(): string | null;

    /** Drop the cached step-up token. Automatic on <c>setSessionToken</c> and on <c>403 STEP_UP_REQUIRED</c>. */
    clearStepUpToken(): void;
}
