import { Navigate, Outlet, Route, Routes, BrowserRouter as Router } from 'react-router-dom';
import AppLayout from './components/layout/app-layout';
import Sidebar from './components/sidebar';
import { GuestRoute, ProtectedRoute } from './components/protectedRoute';
import { AuthProvider } from './hooks/auth';
import './globals.css';
import LoginPage from './pages/auth/login';
import RegisterPage from './pages/auth/register';
import HomePage from './pages/dashboard/home';
import AccountPage from './pages/dashboard/account';
import AutomationPage from './pages/dashboard/automation';
import DevicePanelPage from './pages/dashboard/device';
import LandingPage from './pages/landing';

function DashboardFrame({ mode, showSidebar }: { mode: 'demo' | 'user'; showSidebar: boolean }) {
  const layoutClassName = showSidebar
    ? 'grid gap-6 lg:grid-cols-[280px_minmax(0,1fr)]'
    : 'grid gap-6';

  return (
    <div className={layoutClassName}>
      {showSidebar ? <Sidebar mode={mode} /> : null}
      <main className="min-w-0 space-y-6">
        <Outlet />
      </main>
    </div>
  );
}

function UserDashboardShell() {
  return (
    <ProtectedRoute>
      <DashboardFrame mode="user" showSidebar={false} />
    </ProtectedRoute>
  );
}

function DemoDashboardShell() {
  return <DashboardFrame mode="demo" showSidebar={true} />;
}

function NotFoundPage() {
  return (
    <div className="rt-card flex min-h-[360px] flex-col items-start justify-center gap-4 p-8">
      <div className="rt-kicker">404</div>
      <h1 className="rt-display text-3xl font-semibold">Страница не найдена</h1>
      <p className="max-w-lg text-sm text-[color:var(--muted)]">
        Похоже, ссылка устарела или маршрут ещё не реализован в текущей сборке USmart.
      </p>
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<LandingPage />} />
            <Route path="/auth" element={<Navigate to="/auth/login" replace />} />
            <Route
              path="/auth/login"
              element={(
                <GuestRoute>
                  <LoginPage />
                </GuestRoute>
              )}
            />
            <Route
              path="/auth/register"
              element={(
                <GuestRoute>
                  <RegisterPage />
                </GuestRoute>
              )}
            />
            <Route path="/demo" element={<DemoDashboardShell />}>
              <Route index element={<HomePage mode="demo" />} />
              <Route path="device/:id" element={<DevicePanelPage mode="demo" />} />
            </Route>
            <Route path="/dashboard" element={<UserDashboardShell />}>
              <Route index element={<HomePage mode="user" />} />
              <Route path="automation" element={<AutomationPage />} />
              <Route path="account" element={<AccountPage />} />
              <Route path="device/:id" element={<DevicePanelPage mode="user" />} />
            </Route>
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
