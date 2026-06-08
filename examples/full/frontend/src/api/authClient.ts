export interface LoginResponse {
    sessionToken: string;
    userId: string;
    email: string;
}

export interface TwoFactorChallenge {
    preAuthToken: string;
    expiresAt: string;
    availableMethods: Array<{ id: string; type: string; name: string | null; createdAt: string; lastUsedAt: string | null }>;
}

export type AuthOutcome =
    | { kind: 0; session: LoginResponse; challenge: null }
    | { kind: 1; session: null; challenge: TwoFactorChallenge };

async function postJson<T>(path: string, body: unknown, sessionToken?: string | null): Promise<T> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (sessionToken) headers['Authorization'] = `Bearer ${sessionToken}`;
    const res = await fetch(path, { method: 'POST', headers, body: JSON.stringify(body) });
    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || res.statusText);
    }
    return res.json() as Promise<T>;
}

export const authClient = {
    register: (email: string, password: string) => postJson<LoginResponse>('/auth/register', { email, password }),
    login: (email: string, password: string) => postJson<AuthOutcome>('/auth/login', { email, password }),
    // The server derives the user from the validated pre-auth token — we send it as the bearer, no body.
    finalize: (preAuthToken: string | null) => postJson<LoginResponse>('/auth/finalize', {}, preAuthToken),
    me: async (sessionToken: string) => {
        const res = await fetch('/user/me', { headers: { Authorization: `Bearer ${sessionToken}` } });
        if (!res.ok) throw new Error('unauthorized');
        return res.json() as Promise<{ userId: string; email: string }>;
    },
};
