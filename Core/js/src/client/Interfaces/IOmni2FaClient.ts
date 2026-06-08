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
    TotpEnrollConfirmRequest,
    TotpEnrollStartResponse,
    TwoFactorMethodDto,
    VerifySuccessResponse,
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

    startChallenge(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>>;
    resendChallenge(request: ChallengeResendRequest): Promise<ClientCall<ChallengeStartResponse>>;
    verifyChallenge(request: ChallengeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>>;

    setPreAuthToken(token: string | null): void;
    getPreAuthToken(): string | null;
}
