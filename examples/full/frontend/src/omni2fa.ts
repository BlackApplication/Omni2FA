import { createOmni2Fa, MemoryStorage } from '@omni2fa/core';

// Keep in sync with AuthContext.tsx — both modules use the same key to read/write the host session.
const SESSION_STORAGE_KEY = 'example:session';

function readSessionToken(): string | null {
    const raw = globalThis.localStorage?.getItem(SESSION_STORAGE_KEY);
    if (!raw) return null;
    try {
        const parsed = JSON.parse(raw) as { sessionToken?: string };
        return parsed.sessionToken ?? null;
    } catch {
        return null;
    }
}

/**
 * Custom fetch wired into the Omni2FA core client. The client's own middleware attaches the
 * pre-auth token (for /challenge/*) when one is set. For host-session endpoints (/methods,
 * /enroll/*) no pre-auth is set, so Authorization stays empty after the middleware runs —
 * this wrapper then fills it in with the host's session JWT read from localStorage.
 */
function fetchWithHostSession(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
    if (input instanceof Request) {
        if (!input.headers.has('Authorization')) {
            const token = readSessionToken();
            if (token) input.headers.set('Authorization', `Bearer ${token}`);
        }
        return globalThis.fetch(input);
    }
    const headers = new Headers(init?.headers);
    if (!headers.has('Authorization')) {
        const token = readSessionToken();
        if (token) headers.set('Authorization', `Bearer ${token}`);
    }
    return globalThis.fetch(input, { ...init, headers });
}

export const omni = createOmni2Fa({
    baseUrl: '/api/2fa',
    storage: new MemoryStorage(),
    fetch: fetchWithHostSession,
});
