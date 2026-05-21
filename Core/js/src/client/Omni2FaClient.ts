import createClient from 'openapi-fetch';
import type { paths } from '../types/api';
import type { IStorage } from '../storage/Interfaces/IStorage';
import { MemoryStorage } from '../storage/MemoryStorage';
import { Omni2FaErrorCodes } from '../errors/codes';
import { getDefaultMessage } from '../errors/messages';
import type { ChallengeStartRequest } from '../types/dtos/ChallengeStartRequest';
import type { ChallengeStartResponse } from '../types/dtos/ChallengeStartResponse';
import type { ChallengeVerifyRequest } from '../types/dtos/ChallengeVerifyRequest';
import type { ErrorResponse } from '../types/dtos/ErrorResponse';
import type { MethodCreatedResponse } from '../types/dtos/MethodCreatedResponse';
import type { TotpEnrollConfirmRequest } from '../types/dtos/TotpEnrollConfirmRequest';
import type { TotpEnrollStartResponse } from '../types/dtos/TotpEnrollStartResponse';
import type { TwoFactorMethodDto } from '../types/dtos/TwoFactorMethodDto';
import type { VerifySuccessResponse } from '../types/dtos/VerifySuccessResponse';
import type { ClientCall } from './Interfaces/ClientCall';
import type { IOmni2FaClient } from './Interfaces/IOmni2FaClient';
import type { Omni2FaClientConfig } from './Omni2FaClientConfig';

const DEFAULT_PREAUTH_KEY = 'omni2fa:preauth';

type FetchClient = ReturnType<typeof createClient<paths>>;

export class Omni2FaClient implements IOmni2FaClient {
    private readonly storage: IStorage;
    private readonly preAuthKey: string;
    private readonly inner: FetchClient;

    constructor(config: Omni2FaClientConfig) {
        this.storage = config.storage ?? new MemoryStorage();
        this.preAuthKey = config.preAuthStorageKey ?? DEFAULT_PREAUTH_KEY;
        this.inner = createClient<paths>({
            baseUrl: config.baseUrl,
            fetch: config.fetch ?? globalThis.fetch.bind(globalThis),
        });
        this.inner.use({
            onRequest: ({ request }) => {
                const token = this.getPreAuthToken();
                if (token && !request.headers.has('Authorization')) {
                    request.headers.set('Authorization', `Bearer ${token}`);
                }
                return request;
            },
        });
    }

    setPreAuthToken(token: string | null): void {
        if (token === null || token.length === 0) {
            this.storage.remove(this.preAuthKey);
        } else {
            this.storage.set(this.preAuthKey, token);
        }
    }

    getPreAuthToken(): string | null {
        return this.storage.get(this.preAuthKey);
    }

    async listMethods(): Promise<ClientCall<TwoFactorMethodDto[]>> {
        const { data, error, response } = await this.inner.GET('/methods');
        return this.toCall(data, error, response);
    }

    async removeMethod(methodId: string): Promise<ClientCall<void>> {
        const { error, response } = await this.inner.DELETE('/methods/{methodId}', { params: { path: { methodId } } });
        if (error) {
            return this.errorCall(error, response);
        }
        return { ok: true, value: undefined };
    }

    async startTotpEnrollment(): Promise<ClientCall<TotpEnrollStartResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/totp/start');
        return this.toCall(data, error, response);
    }

    async confirmTotpEnrollment(request: TotpEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/totp/confirm', { body: request });
        return this.toCall(data, error, response);
    }

    async startChallenge(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/start', { body: request });
        return this.toCall(data, error, response);
    }

    async verifyChallenge(request: ChallengeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/verify', { body: request });
        return this.toCall(data, error, response);
    }

    private toCall<T>(data: T | undefined, error: ErrorResponse | undefined, response: Response): ClientCall<T> {
        if (error !== undefined) {
            return this.errorCall(error, response);
        }
        if (data === undefined) {
            return {
                ok: false,
                code: Omni2FaErrorCodes.NetworkError,
                message: getDefaultMessage(Omni2FaErrorCodes.NetworkError),
                httpStatus: response.status,
            };
        }
        return { ok: true, value: data };
    }

    private errorCall(error: ErrorResponse, response: Response): ClientCall<never> {
        const code = error.code || Omni2FaErrorCodes.Unknown;
        return {
            ok: false,
            code,
            message: error.message || getDefaultMessage(code),
            httpStatus: response.status,
            details: error.details ?? null,
        };
    }
}
