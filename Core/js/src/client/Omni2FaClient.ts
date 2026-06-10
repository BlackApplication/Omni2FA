import createClient from 'openapi-fetch';
import type { paths } from '../types/api';
import type { IStorage } from '../storage/Interfaces/IStorage';
import { MemoryStorage } from '../storage/MemoryStorage';
import { Omni2FaErrorCodes } from '../errors/codes';
import { getDefaultMessage } from '../errors/messages';
import type {
    ChallengeResendRequest,
    ChallengeStartRequest,
    ChallengeStartResponse,
    ChallengeVerifyRequest,
    EmailEnrollConfirmRequest,
    EmailEnrollResendRequest,
    EmailEnrollStartRequest,
    EmailEnrollStartResponse,
    ErrorResponse,
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
} from '../types/dtos';
import type { ClientCall } from './Interfaces/ClientCall';
import type { IOmni2FaClient, StepUpHandler } from './Interfaces/IOmni2FaClient';
import type { Omni2FaClientConfig } from './Omni2FaClientConfig';
import { STEP_UP_HEADER } from '../stepup/constants';

const DEFAULT_PREAUTH_KEY = 'omni2fa:preauth';
const DEFAULT_SESSION_KEY = 'omni2fa:session';
// Only used to resolve a relative baseUrl/request URL so we can read its pathname; the origin is irrelevant.
const FALLBACK_ORIGIN = 'http://omni2fa.local';

type FetchClient = ReturnType<typeof createClient<paths>>;

export class Omni2FaClient implements IOmni2FaClient {
    private readonly storage: IStorage;
    private readonly preAuthKey: string;
    private readonly sessionKey: string;
    private readonly basePath: string;
    private readonly inner: FetchClient;
    private stepUpHandler: StepUpHandler | null = null;

    constructor(config: Omni2FaClientConfig) {
        this.storage = config.storage ?? new MemoryStorage();
        this.preAuthKey = config.preAuthStorageKey ?? DEFAULT_PREAUTH_KEY;
        this.sessionKey = config.sessionStorageKey ?? DEFAULT_SESSION_KEY;
        // Mount path of the API, e.g. "/api/2fa" — used to classify endpoints by their path under it.
        this.basePath = new URL(config.baseUrl, FALLBACK_ORIGIN).pathname.replace(/\/$/, '');
        this.inner = createClient<paths>({
            baseUrl: config.baseUrl,
            fetch: config.fetch ?? globalThis.fetch.bind(globalThis),
            ...(config.credentials ? { credentials: config.credentials } : {}),
        });
        this.inner.use({
            onRequest: ({ request }) => {
                // A custom fetch / host-set header always wins.
                if (request.headers.has('Authorization')) {
                    return request;
                }
                // Pre-auth token guards the 2FA ceremony; everything else uses the host session token.
                const token = this.isPreAuthEndpoint(request.url) ? this.getPreAuthToken() : this.getSessionToken();
                if (token) {
                    request.headers.set('Authorization', `Bearer ${token}`);
                }
                return request;
            },
        });
    }

    /** Pre-auth endpoints are exactly the ones mounted under <c>{basePath}/challenge/</c>. */
    private isPreAuthEndpoint(url: string): boolean {
        const path = new URL(url, FALLBACK_ORIGIN).pathname;
        const relative = path.startsWith(this.basePath) ? path.slice(this.basePath.length) : path;
        return relative.startsWith('/challenge/');
    }

    setPreAuthToken(token: string | null): void {
        this.setToken(this.preAuthKey, token);
    }

    getPreAuthToken(): string | null {
        return this.storage.get(this.preAuthKey);
    }

    setSessionToken(token: string | null): void {
        this.setToken(this.sessionKey, token);
    }

    getSessionToken(): string | null {
        return this.storage.get(this.sessionKey);
    }

    setStepUpHandler(handler: StepUpHandler | null): void {
        this.stepUpHandler = handler;
    }

    /**
     * Run a request and, if it comes back 403 STEP_UP_REQUIRED with a handler registered, confirm 2FA
     * and retry once with the step-up header. Used by the library's own sensitive endpoints; other calls
     * invoke openapi-fetch directly.
     */
    private async sendWithStepUp<T>(
        invoke: (headers: Record<string, string>) => Promise<{ data?: T; error?: ErrorResponse; response: Response }>,
    ): Promise<{ data?: T; error?: ErrorResponse; response: Response }> {
        const first = await invoke({});
        if (
            first.error !== undefined &&
            first.response.status === 403 &&
            first.error.code === Omni2FaErrorCodes.StepUpRequired &&
            this.stepUpHandler !== null
        ) {
            const methods = (first.error.details?.availableMethods as TwoFactorMethodDto[] | undefined) ?? [];
            const token = await this.stepUpHandler(methods);
            if (token) {
                return invoke({ [STEP_UP_HEADER]: token });
            }
        }
        return first;
    }

    private setToken(key: string, token: string | null): void {
        if (token === null || token.length === 0) {
            this.storage.remove(key);
        } else {
            this.storage.set(key, token);
        }
    }

    async listMethods(): Promise<ClientCall<TwoFactorMethodDto[]>> {
        const { data, error, response } = await this.inner.GET('/methods');
        return this.toCall(data, error, response);
    }

    async removeMethod(methodId: string): Promise<ClientCall<void>> {
        const { error, response } = await this.sendWithStepUp((headers) =>
            this.inner.DELETE('/methods/{methodId}', { params: { path: { methodId } }, headers }),
        );
        if (error) {
            return this.errorCall(error, response);
        }
        return { ok: true, value: undefined };
    }

    async startTotpEnrollment(): Promise<ClientCall<TotpEnrollStartResponse>> {
        const { data, error, response } = await this.sendWithStepUp((headers) => this.inner.POST('/enroll/totp/start', { headers }));
        return this.toCall(data, error, response);
    }

    async confirmTotpEnrollment(request: TotpEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/totp/confirm', { body: request });
        return this.toCall(data, error, response);
    }

    async startEmailEnrollment(request: EmailEnrollStartRequest): Promise<ClientCall<EmailEnrollStartResponse>> {
        const { data, error, response } = await this.sendWithStepUp((headers) => this.inner.POST('/enroll/email/start', { body: request, headers }));
        return this.toCall(data, error, response);
    }

    async confirmEmailEnrollment(request: EmailEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/email/confirm', { body: request });
        return this.toCall(data, error, response);
    }

    async resendEmailEnrollment(request: EmailEnrollResendRequest): Promise<ClientCall<EmailEnrollStartResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/email/resend', { body: request });
        return this.toCall(data, error, response);
    }

    async startWebAuthnEnrollment(): Promise<ClientCall<WebAuthnEnrollStartResponse>> {
        const { data, error, response } = await this.sendWithStepUp((headers) => this.inner.POST('/enroll/webauthn/start', { headers }));
        return this.toCall(data, error, response);
    }

    async confirmWebAuthnEnrollment(request: WebAuthnEnrollConfirmRequest): Promise<ClientCall<MethodCreatedResponse>> {
        const { data, error, response } = await this.inner.POST('/enroll/webauthn/confirm', { body: request });
        return this.toCall(data, error, response);
    }

    async startChallenge(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/start', { body: request });
        return this.toCall(data, error, response);
    }

    async resendChallenge(request: ChallengeResendRequest): Promise<ClientCall<ChallengeStartResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/resend', { body: request });
        return this.toCall(data, error, response);
    }

    async verifyChallenge(request: ChallengeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/verify', { body: request });
        return this.toCall(data, error, response);
    }

    async verifyRecoveryCode(request: RecoveryCodeVerifyRequest): Promise<ClientCall<VerifySuccessResponse>> {
        const { data, error, response } = await this.inner.POST('/challenge/recovery-code', { body: request });
        return this.toCall(data, error, response);
    }

    async startStepUp(request: ChallengeStartRequest): Promise<ClientCall<ChallengeStartResponse>> {
        const { data, error, response } = await this.inner.POST('/stepup/start', { body: request });
        return this.toCall(data, error, response);
    }

    async resendStepUp(request: ChallengeResendRequest): Promise<ClientCall<ChallengeStartResponse>> {
        const { data, error, response } = await this.inner.POST('/stepup/resend', { body: request });
        return this.toCall(data, error, response);
    }

    async verifyStepUp(request: ChallengeVerifyRequest): Promise<ClientCall<StepUpVerifyResponse>> {
        const { data, error, response } = await this.inner.POST('/stepup/verify', { body: request });
        return this.toCall(data, error, response);
    }

    async regenerateRecoveryCodes(): Promise<ClientCall<RecoveryCodesResponse>> {
        const { data, error, response } = await this.sendWithStepUp((headers) => this.inner.POST('/recovery-codes/regenerate', { headers }));
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
