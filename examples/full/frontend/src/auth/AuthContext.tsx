import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authClient, type LoginResponse } from '../api/authClient';
import { omni } from '../omni2fa';

interface AuthState {
    session: LoginResponse | null;
    setSession: (s: LoginResponse | null) => void;
    logout: () => void;
    authFetch: (path: string, init?: RequestInit) => Promise<Response>;
}

export const AuthContext = createContext<AuthState | null>(null);

const STORAGE_KEY = 'example:session';

function readStored(): LoginResponse | null {
    const raw = globalThis.localStorage?.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
        return JSON.parse(raw) as LoginResponse;
    } catch {
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSessionRaw] = useState<LoginResponse | null>(readStored);

    const setSession = useCallback((next: LoginResponse | null) => {
        setSessionRaw(next);
        if (next) globalThis.localStorage?.setItem(STORAGE_KEY, JSON.stringify(next));
        else globalThis.localStorage?.removeItem(STORAGE_KEY);
    }, []);

    // Keep the Omni2FA client's host-session token in sync — it attaches it to /methods, /enroll/*, /recovery-codes/*.
    useEffect(() => {
        omni.client.setSessionToken(session?.sessionToken ?? null);
    }, [session]);

    const logout = useCallback(() => setSession(null), [setSession]);

    const authFetch = useCallback(
        (path: string, init?: RequestInit) => {
            const headers = new Headers(init?.headers);
            if (session?.sessionToken) headers.set('Authorization', `Bearer ${session.sessionToken}`);
            return fetch(path, { ...init, headers });
        },
        [session],
    );

    void authClient;
    const value = useMemo<AuthState>(() => ({ session, setSession, logout, authFetch }), [session, setSession, logout, authFetch]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
