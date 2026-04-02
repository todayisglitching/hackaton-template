import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/auth';

interface RouteProps {
  children: ReactNode;
}

function RouteLoader() {
  return (
    <div className="rt-card flex min-h-[320px] items-center justify-center p-8 text-sm text-[color:var(--muted)]">
      Проверяем сессию...
    </div>
  );
}

export function ProtectedRoute({ children }: RouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <RouteLoader />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace />;
  }

  return children;
}

export function GuestRoute({ children }: RouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <RouteLoader />;
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}
