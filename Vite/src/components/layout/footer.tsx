import { NavLink } from 'react-router-dom';

const Footer = () => {
  return (
    <footer className="relative z-10 mx-auto w-full max-w-7xl px-6 pb-8 pt-10 text-sm text-[color:var(--muted)] md:px-8">
      <div className="rt-divider mb-6" />
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>© 2026 USmart. Ростелеком. Универсальная платформа умного дома.</div>
        <div className="flex flex-wrap gap-4">
          <NavLink to="/demo" className="transition-colors hover:text-[color:var(--foreground)]">
            Демо
          </NavLink>
          <NavLink to="/auth/login" className="transition-colors hover:text-[color:var(--foreground)]">
            Войти
          </NavLink>
          <NavLink to="/auth/register" className="transition-colors hover:text-[color:var(--foreground)]">
            Регистрация
          </NavLink>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
