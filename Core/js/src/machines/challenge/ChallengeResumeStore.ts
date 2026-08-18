import type { IStorage } from '../../storage/Interfaces/IStorage';
import type { ChallengeResumeState } from './ChallengeResumeState';

export class ChallengeResumeStore {
    private readonly storage: IStorage;
    private readonly key: string;

    constructor(storage: IStorage, key: string) {
        this.storage = storage;
        this.key = key;
    }

    read(): ChallengeResumeState | null {
        const raw = this.storage.get(this.key);
        if (raw === null) {
            return null;
        }
        const parsed = parse(raw);
        if (parsed === null) {
            this.clear();
        }
        return parsed;
    }

    write(state: ChallengeResumeState): void {
        this.storage.set(this.key, JSON.stringify(state));
    }

    clear(): void {
        this.storage.remove(this.key);
    }
}

function parse(raw: string): ChallengeResumeState | null {
    let value: unknown;
    try {
        value = JSON.parse(raw);
    } catch {
        return null;
    }
    if (typeof value !== 'object' || value === null) {
        return null;
    }
    const candidate = value as Record<string, unknown>;
    if (typeof candidate.methodId !== 'string' || candidate.methodId.length === 0) {
        return null;
    }
    if (candidate.methodType !== 'Totp' && candidate.methodType !== 'Email' && candidate.methodType !== 'WebAuthn') {
        return null;
    }
    return {
        methodId: candidate.methodId,
        methodType: candidate.methodType,
        expiresAt: typeof candidate.expiresAt === 'string' ? candidate.expiresAt : null,
        resendAvailableAt: typeof candidate.resendAvailableAt === 'string' ? candidate.resendAvailableAt : null,
    };
}
