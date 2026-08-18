import createClient from 'openapi-fetch';
import type { paths } from '../types/api';
import type { IStorage } from '../storage/Interfaces/IStorage';
import { createDefaultStorage } from '../storage/createDefaultStorage';
import { storageKey } from '../storage/storageKey';
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

/**
 * Extract the pathname from a URL string without constructing a `URL` (which would need an absolute
 * base for relative inputs, forcing a dummy origin literal into the bundle). Handles absolute,
 * protocol-relative, and bare-path URLs, and strips any query string or fragment. The origin is
 * irrelevant here — callers only classify endpoints by their path under {basePath}.
 */
function pathnameOf(input: string): string {
    let s = input;
    const scheme = /^[a-zA-Z][a-zA-Z\d+\-.]*:\/\//.exec(s);
    if (scheme) {
        s = s.slice(scheme[0].length);
        const slash = s.indexOf('/');
        s = slash === -1 ? '/' : s.slice(slash);
    } else if (s.startsWith('//')) {
        s = s.slice(2);
        const slash = s.indexOf('/');
        s = slash === -1 ? '/' : s.slice(slash);
    }
    const hash = s.indexOf('#');
    if (hash !== -1) s = s.slice(0, hash);
    const query = s.indexOf('?');
    if (query !== -1) s = s.slice(0, query);
    return s.startsWith('/') ? s : `/${s}`;
}

type FetchClient = ReturnType<typeof createClient<paths>>;

export class Omni2FaClient implements IOmni2FaClient {
    private readonly storage: IStorage;
    private readonly preAuthKey: string;
    private readonly sessionKey: string;
    private readonly basePath: string;
    private readonly inner: FetchClient;
    private stepUpHandler: StepUpHandler | null = null;
    // Never in `storage` — a reusable confirmation must not outlive the tab.
    private cachedStepUp: { token: string; expiresAt: number } | null = null;

    constructor(config: Omni2FaClientConfig) {
        this.storage = config.storage ?? createDefaultStorage();
        this.preAuthKey = config.preAuthStorageKey ?? storageKey(config.namespace, 'preauth');
        this.sessionKey = config.sessionStorageKey ?? storageKey(config.namespace, 'session');
        // Mount path of the API, e.g. "/api/2fa" — used to classify endpoints by their path under it.
        this.basePath = pathnameOf(config.baseUrl).replace(/\/$/, '');
        this.inner = createClient<paths>({
            baseUrl: config.baseUrl,
            fetch: config.fetch ?? globalThis.fetch.bind(globalThis),
            ...(config.credentials ? { credentials: config.credentials } : {}),
        });
        // Registered before the auth middleware so a host-supplied Authorization header still wins.
        if (config.headers) {
            const configured = config.headers;
            const resolveHeaders = typeof configured === 'function' ? configured : () => configured;
            this.inner.use({
                onRequest: ({ request }) => {
                    const extra = resolveHeaders();
                    if (extra) {
                        new Headers(extra).forEach((value, key) => request.headers.set(key, value));
                    }
                    return request;
                },
            });
        }
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
        const path = pathnameOf(url);
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
        // Only on sign-out: a login grants its own confirmation just before the host sets the session,
        // and clearing unconditionally here would throw that away. Swapping users is safe without it —
        // the server binds every token to its subject and rejects it for anyone else.
        if (token === null || token.length === 0) {
            this.clearStepUpToken();
        }
        this.setToken(this.sessionKey, token);
    }

    getSessionToken(): string | null {
        return this.storage.get(this.sessionKey);
    }

    setStepUpHandler(handler: StepUpHandler | null): void {
        this.stepUpHandler = handler;
    }

    peekStepUpToken(): string | null {
        if (this.cachedStepUp === null) {
            return null;
        }
        if (Date.now() >= this.cachedStepUp.expiresAt) {
            this.cachedStepUp = null;
            return null;
        }
        return this.cachedStepUp.token;
    }

    clearStepUpToken(): void {
        this.cachedStepUp = null;
    }

    /**
     * Remember a confirmation for as long as the server says it stays usable. The server is the only
     * source of that instant, so there is nothing to configure here and nothing to keep in sync.
     */
    private cacheStepUpToken(token: string, graceUntil: string | undefined): void {
        if (graceUntil === undefined) {
            return;
        }
        const expiresAt = Date.parse(graceUntil);
        if (Number.isNaN(expiresAt) || expiresAt <= Date.now()) {
            return;
        }
        this.cachedStepUp = { token, expiresAt };
    }

    /**
     * Run a request and, if it comes back 403 STEP_UP_REQUIRED with a handler registered, confirm 2FA
     * and retry once with the step-up header. Used by the library's own sensitive endpoints; other calls
     * invoke openapi-fetch directly. A confirmation still inside the grace window is attached up front,
     * so a run of sensitive actions costs one prompt.
     */
    private async sendWithStepUp<T>(
        invoke: (headers: Record<string, string>) => Promise<{ data?: T; error?: ErrorResponse; response: Response }>,
    ): Promise<{ data?: T; error?: ErrorResponse; response: Response }> {
        const cached = this.peekStepUpToken();
        const first = await invoke(cached ? { [STEP_UP_HEADER]: cached } : {});
        if (
            first.error !== undefined &&
            first.response.status === 403 &&
            first.error.code === Omni2FaErrorCodes.StepUpRequired
        ) {
            // The server did not accept what we had — drop it so the prompt comes next, here and in adapters.
            this.clearStepUpToken();
            if (this.stepUpHandler !== null) {
                const methods = (first.error.details?.availableMethods as TwoFactorMethodDto[] | undefined) ?? [];
                const token = await this.stepUpHandler(methods);
                if (token) {
                    return invoke({ [STEP_UP_HEADER]: token });
                }
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
        // The login just proved 2FA — carry that into the step-up barrier if the backend grants it.
        if (data?.stepUpToken !== undefined) {
            this.cacheStepUpToken(data.stepUpToken, data.stepUpGraceUntil);
        }
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
        if (data !== undefined) {
            this.cacheStepUpToken(data.stepUpToken, data.graceUntil);
        }
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
