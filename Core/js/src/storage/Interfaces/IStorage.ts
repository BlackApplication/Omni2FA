/**
 * Persistence boundary for short-lived 2FA-related values (most notably the pre-auth token).
 * Implementations choose where to keep values: in-memory, sessionStorage, localStorage, cookies, etc.
 */
export interface IStorage {
    get(key: string): string | null;
    set(key: string, value: string): void;
    remove(key: string): void;
}
