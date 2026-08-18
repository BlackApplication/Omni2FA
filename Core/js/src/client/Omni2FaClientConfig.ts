import type { IStorage } from '../storage/Interfaces/IStorage';

/** Constructor configuration for <see cref="Omni2FaClient"/>. */
export interface Omni2FaClientConfig {
    /** Origin + mount path of the Omni2FA backend, e.g. <c>https://app.example.com/api/2fa</c>. */
    baseUrl: string;
    /**
     * Where the pre-auth token and the resumable challenge are kept. Defaults to `sessionStorage`, so a
     * reloaded or restored tab returns to the code screen instead of the login form; falls back to
     * in-memory where `sessionStorage` is unavailable or throws (SSR, Safari private mode). Pass
     * `new MemoryStorage()` to opt out and keep everything in the JS heap.
     */
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
    /**
     * Namespace for this client's storage keys — <c>omni2fa:{namespace}:preauth</c> instead of
     * <c>omni2fa:preauth</c>. Set it when the app builds more than one client (a staff area and a customer
     * portal, each with its own login) and the storage is shared, e.g. <c>sessionStorage</c>: without it
     * both clients read and write the same key and the second login overwrites the first one's token.
     * Ignored for keys given explicitly below.
     */
    namespace?: string;
    /** Storage key used to persist the pre-auth token. Defaults to <c>omni2fa:preauth</c>, or <c>omni2fa:{namespace}:preauth</c>. */
    preAuthStorageKey?: string;
    /** Storage key used to persist the host session token. Defaults to <c>omni2fa:session</c>, or <c>omni2fa:{namespace}:session</c>. */
    sessionStorageKey?: string;

    /** Storage key used to persist the resumable challenge. Defaults to `omni2fa:challenge`, or `omni2fa:{namespace}:challenge`. */
    challengeStorageKey?: string;
}
