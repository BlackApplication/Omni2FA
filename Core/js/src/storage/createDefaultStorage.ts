import type { IStorage } from './Interfaces/IStorage';
import { MemoryStorage } from './MemoryStorage';
import { SessionStorageStorage } from './SessionStorageStorage';

const PROBE_KEY = 'omni2fa:probe';

export function createDefaultStorage(): IStorage {
    try {
        const session = globalThis.sessionStorage;
        if (!session) {
            return new MemoryStorage();
        }
        // Reading the object is not enough: Safari in private mode throws only on write.
        session.setItem(PROBE_KEY, '1');
        session.removeItem(PROBE_KEY);
        return new SessionStorageStorage();
    } catch {
        return new MemoryStorage();
    }
}
