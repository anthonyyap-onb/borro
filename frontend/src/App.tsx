import { GoogleOAuthProvider } from '@react-oauth/google';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './features/auth/AuthContext';
import { LoginPage } from './features/auth/LoginPage';
import { ProtectedRoute } from './features/auth/ProtectedRoute';
import { RegisterPage } from './features/auth/RegisterPage';

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '';

function DashboardPage() {
  const { user, logout } = useAuth();
  return (
    <div className="min-h-screen bg-surface flex items-center justify-center font-body">
      <div className="text-center">
        <h1 className="font-headline text-4xl font-extrabold text-primary mb-2">
          Welcome, {user?.firstName}!
        </h1>
        <p className="text-on-surface-variant mb-8">More features coming soon.</p>
        <button
          onClick={logout}
          className="px-6 py-3 bg-primary text-on-primary rounded-full font-bold hover:opacity-90 transition-opacity"
        >
          Log Out
        </button>
      </div>
    </div>
  );
}

function App() {
  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <DashboardPage />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </GoogleOAuthProvider>
  );
}

export default App;

