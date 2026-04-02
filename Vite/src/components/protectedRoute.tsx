import type { ReactNode } from 'react';
import { useAuth } from '../hooks/auth';
import { Navigate } from 'react-router-dom';

interface RouteProps {
    children: ReactNode;
}

export const ProtectedRoute = ({ children }: RouteProps) => {
    const { token } = useAuth();

    if (!token) {
        return <Navigate to="/auth/login" replace />;
    }

    return children;
};

export const GuestRoute = ({ children }: RouteProps) => {
    const { token } = useAuth();

    if (token) {
        return <Navigate to="/dashboard" replace />;
    }

    return children;
};
