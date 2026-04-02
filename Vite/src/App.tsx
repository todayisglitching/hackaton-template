// App.js
import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import LandingPage from './pages/landing';
import LoginPage from './pages/auth/login';
import RegisterPage from './pages/auth/register';
import HomePage from './pages/dashboard/home';
import AccountPage from './pages/dashboard/account';
import Sidebar from './components/sidebar';
import './globals.css';
import { AuthProvider } from './hooks/auth';
import { ProtectedRoute, GuestRoute } from './components/protectedRoute';

function App() {
    return (
        <AuthProvider>
            <Router>
                <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/auth" element={<Navigate to="/auth/login" replace />} />
                    <Route path="/auth/login" element={<GuestRoute><LoginPage /></GuestRoute>} />
                    <Route path="/auth/register" element={<GuestRoute><RegisterPage /></GuestRoute>} />


                    <Route
                        path="/dashboard"
                        element={
                            <ProtectedRoute>
                                <div style={{ display: 'flex' }}>
                                    <Sidebar />
                                    <main style={{ flex: 1, padding: '20px' }}>
                                        <Routes>
                                            <Route path="account" element={<AccountPage />} />
                                            <Route path="" element={<HomePage />} />
                                        </Routes>
                                    </main>
                                </div>
                            </ProtectedRoute>
                        }
                    />

                    <Route path="*" element={<div>Страница не найдена</div>} />
                </Routes>
            </Router>
        </AuthProvider>
    );
}

export default App;
