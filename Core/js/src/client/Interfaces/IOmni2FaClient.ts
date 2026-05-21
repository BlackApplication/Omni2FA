import type { ChallengeStartRequest } from '../../types/dtos/ChallengeStartRequest';
import type { ChallengeStartResponse } from '../../types/dtos/ChallengeStartResponse';
import type { ChallengeVerifyRequest } from '../../types/dtos/ChallengeVerifyRequest';
import type { MethodCreatedResponse } from '../../types/dtos/MethodCreatedResponse';
import type { TotpEnrollConfirmRequest } from '../../types/dtos/TotpEnrollConfirmRequest';
import type { TotpEnrollStartResponse } from '../../types/dtos/TotpEnrollStartResponse';
import type { TwoFactorMethodDto } from '../../types/dtos/TwoFactorMethodDto';
import type { VerifySuccessResponse } from '../../types/dtos/VerifySuccessResponse';
import type { ClientCall } from './ClientCall';

/** Typed wrapper over the Omni2FA HTTP contract. Adapters consume this — they never call <c>fetch</c> directly. */
export interface IOmni2FaClient {
    listMethods(): Promise<ClientCall<TwoFactorMethodDto[]>>;
    removeMethod(methodId: string): Promise<ClientCall<void>>;

    startTotpEnrollment(): Promise<ClientCall<TotpEnrollStartResponse>>;
    confirmTotpEnrollment(request: TotpEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>>;

    startChallenge(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>>;
    verifyChallenge(request: ChallengeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>>;

    setPreAuthToken(token: string | null): void;
    getPreAuthToken(): string | null;
}
