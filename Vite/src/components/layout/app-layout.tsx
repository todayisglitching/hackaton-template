import { Outlet, useLocation } from 'react-router-dom';
import Footer from './footer';
import Header from './header';

const AppLayout = () => {
  const location = useLocation();
  const isLanding = location.pathname === '/';
  const isAuth = location.pathname.startsWith('/auth');
  const isDashboard = location.pathname.startsWith('/dashboard');

  return (
    <div className={`rt-landing min-h-screen ${isDashboard ? 'rt-dashboard-bg' : ''}`}>
      <div className="rt-mesh" aria-hidden="true" />
      {isLanding ? (
        <>
          <div className="rt-grid absolute inset-0 pointer-events-none" aria-hidden="true" />
          <div className="rt-orbit" aria-hidden="true" />
        </>
      ) : null}
      <Header />
      <main
        className={`relative z-10 mx-auto w-full max-w-7xl px-6 md:px-8 ${
          isAuth ? 'pb-8 pt-8 md:pt-10' : 'pb-12 pt-10 md:pt-14'
        }`}
      >
        <div className={isAuth ? 'mx-auto max-w-5xl' : ''}>
          <Outlet />
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default AppLayout;
