import { Button, Chip } from '@heroui/react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/auth';
import BrandLogo from '../brand-logo';

const landingLinks = [
  { href: '#problem', label: 'Вызов' },
  { href: '#solution', label: 'Решение' },
  { href: '#tech', label: 'Стек' },
  { href: '#demo', label: 'Демо' },
];

const userLinks = [
  { to: '/dashboard', label: 'Устройства', exact: true },
  { to: '/dashboard/automation', label: 'Автоматизации' },
  { to: '/dashboard/account', label: 'Аккаунт' },
];
const demoLinks = [
  { to: '/demo', label: 'Демо', exact: true },
];

const Header = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const isLanding = location.pathname === '/';
  const isDashboard = location.pathname.startsWith('/dashboard');
  const isDemo = location.pathname.startsWith('/demo');
  const navItems = isDashboard ? userLinks : isDemo ? demoLinks : [];

  return (
    <header className="relative z-10 mx-auto w-full max-w-7xl px-6 pt-6 md:px-8 md:pt-8">
      <div className="rt-topbar">
        <NavLink to="/" className="flex items-center gap-3 no-underline transition-opacity hover:opacity-80">
          <div className="rt-logo-badge">
            <BrandLogo className="h-10 w-10" />
          </div>
          <div>
            <div className="text-sm font-semibold tracking-[0.14em] text-[color:var(--accent)]">USmart</div>
            <div className="text-sm text-[color:var(--muted)]">Управление, понятное каждому девайсу</div>
          </div>
        </NavLink>

        {isLanding ? (
          <nav className="hidden items-center gap-5 text-sm lg:flex">
            {landingLinks.map((item) => (
              <a key={item.href} href={item.href} className="text-[color:var(--muted)] transition-colors hover:text-[color:var(--foreground)]">
                {item.label}
              </a>
            ))}
          </nav>
        ) : null}

        {isDashboard || isDemo ? (
          <nav className="hidden items-center gap-2 lg:flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.exact}
                className={({ isActive }) => `rt-nav-link ${isActive ? 'rt-nav-link-active' : ''}`}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        ) : null}

        <div className="flex items-center gap-3">
          {isLanding ? (
            <>
              <Button className="rt-btn rt-btn-secondary" onClick={() => navigate('/auth/login')}>
                Войти
              </Button>
              <Button className="rt-btn rt-btn-primary" onClick={() => navigate('/auth/register')}>
                Регистрация
              </Button>
            </>
          ) : isDemo ? (
            <Button className="rt-btn rt-btn-secondary" onClick={() => navigate('/auth/login')}>
              В личный кабинет
            </Button>
          ) : isDashboard ? (
            <>
              <Chip className="rt-pill max-w-[220px] truncate text-xs hidden md:inline-flex">
                {user?.email ?? localStorage.getItem('userEmail') ?? 'Пользователь'}
              </Chip>
              <Button className="rt-btn rt-btn-secondary" onClick={() => void logout()}>
                Выйти
              </Button>
            </>
          ) : null}
        </div>
      </div>
    </header>
  );
};

export default Header;
