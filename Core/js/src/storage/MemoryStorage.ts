import type { IStorage } from './Interfaces/IStorage';

/** In-process storage. Default. Values vanish on page reload — safest default. */
export class MemoryStorage implements IStorage {
    private readonly map = new Map<string, string>();

    get(key: string): string | null {
        return this.map.get(key) ?? null;
    }

    set(key: string, value: string): void {
        this.map.set(key, value);
    }

    remove(key: string): void {
        this.map.delete(key);
    }
}
