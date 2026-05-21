import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

export function ProtectedRoute({ children }: { children: ReactNode }) {
    const { session } = useAuth();
    if (!session) return <Navigate to="/login" replace />;
    return <>{children}</>;
}
