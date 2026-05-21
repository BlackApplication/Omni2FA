import type { IStorage } from '../storage/Interfaces/IStorage';

/** Constructor configuration for <see cref="Omni2FaClient"/>. */
export interface Omni2FaClientConfig {
    /** Origin + mount path of the Omni2FA backend, e.g. <c>https://app.example.com/api/2fa</c>. */
    baseUrl: string;
    /** Storage for the pre-auth token. Defaults to in-memory — lost on page reload. */
    storage?: IStorage;
    /** Optional fetch override (custom interceptors, retry policy, etc.). Defaults to global <c>fetch</c>. */
    fetch?: typeof fetch;
    /** Storage key used to persist the pre-auth token. Defaults to <c>omni2fa:preauth</c>. */
    preAuthStorageKey?: string;
}
