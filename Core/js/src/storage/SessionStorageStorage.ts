import type { IStorage } from './Interfaces/IStorage';

/** Browser <c>sessionStorage</c>. Values persist across page reloads in the same tab, gone when tab closes. */
export class SessionStorageStorage implements IStorage {
    get(key: string): string | null {
        return globalThis.sessionStorage?.getItem(key) ?? null;
    }

    set(key: string, value: string): void {
        globalThis.sessionStorage?.setItem(key, value);
    }

    remove(key: string): void {
        globalThis.sessionStorage?.removeItem(key);
    }
}
