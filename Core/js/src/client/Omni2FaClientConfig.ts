import type { IStorage } from '../storage/Interfaces/IStorage';

/** Constructor configuration for <see cref="Omni2FaClient"/>. */
export interface Omni2FaClientConfig {
    /** Origin + mount path of the Omni2FA backend, e.g. <c>https://app.example.com/api/2fa</c>. */
    baseUrl: string;
    /** Storage for the pre-auth token. Defaults to in-memory — lost on page reload. */
    storage?: IStorage;
    /** Optional fetch override (custom interceptors, retry policy, etc.). Defaults to global <c>fetch</c>. */
    fetch?: typeof fetch;
    /**
     * Cookie policy for requests. Set <c>'include'</c> when host-session auth is a cookie the browser
     * should attach (instead of a Bearer token via <c>setSessionToken</c>). Default <c>'same-origin'</c>.
     */
    credentials?: 'omit' | 'same-origin' | 'include';
    /** Storage key used to persist the pre-auth token. Defaults to <c>omni2fa:preauth</c>. */
    preAuthStorageKey?: string;
    /** Storage key used to persist the host session token. Defaults to <c>omni2fa:session</c>. */
    sessionStorageKey?: string;
}
