const KEY_PREFIX = 'omni2fa';

export type StorageRole = 'preauth' | 'session' | 'challenge';

export function storageKey(namespace: string | undefined, role: StorageRole): string {
    return namespace ? `${KEY_PREFIX}:${namespace}:${role}` : `${KEY_PREFIX}:${role}`;
}
