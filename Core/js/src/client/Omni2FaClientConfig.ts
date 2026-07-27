import type { IStorage } from '../storage/Interfaces/IStorage';

/** Constructor configuration for <see cref="Omni2FaClient"/>. */
export interface Omni2FaClientConfig {
    /** Origin + mount path of the Omni2FA backend, e.g. <c>https://app.example.com/api/2fa</c>. */
    baseUrl: string;
    /** Storage for the pre-auth token. Defaults to in-memory — lost on page reload. */
    storage?: IStorage;
    /**
     * Extra headers attached to every request — a routing flag, <c>Accept-Language</c>, an active-tenant id.
     * Prefer this over <c>fetch</c> for headers: it composes with the client's own auth header instead of
     * replacing the transport. Pass a function to resolve them per request (a language the user can switch,
     * a tenant selected at runtime). A header set here wins over the one the client would add itself.
     */
    headers?: HeadersInit | (() => HeadersInit | null | undefined);
    /**
     * Optional transport override — retry policy, logging, a non-browser fetch. Defaults to global <c>fetch</c>.
     * It is called with a ready-made <c>Request</c> (never a URL + init pair): forward that object as-is, e.g.
     * <c>(input, init) => fetch(input, init)</c>. Rebuilding it from <c>init</c> drops the headers, body and
     * credentials the client already set. For headers alone use <c>headers</c> above, not this.
     */
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
