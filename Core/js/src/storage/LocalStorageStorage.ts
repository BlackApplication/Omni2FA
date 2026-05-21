import type { IStorage } from './Interfaces/IStorage';

/** Browser <c>localStorage</c>. Values persist indefinitely across sessions. Use with caution for sensitive data. */
export class LocalStorageStorage implements IStorage {
    get(key: string): string | null {
        return globalThis.localStorage?.getItem(key) ?? null;
    }

    set(key: string, value: string): void {
        globalThis.localStorage?.setItem(key, value);
    }

    remove(key: string): void {
        globalThis.localStorage?.removeItem(key);
    }
}
