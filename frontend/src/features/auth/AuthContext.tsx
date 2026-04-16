import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';

export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AuthResult {
  token: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (result: AuthResult) => void;
  logout: () => void;
}

const TOKEN_KEY = 'borro_token';
const USER_KEY = 'borro_user';

function loadFromStorage(): { token: string | null; user: AuthUser | null } {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    const raw = localStorage.getItem(USER_KEY);
    const user = raw ? (JSON.parse(raw) as AuthUser) : null;
    return { token, user };
  } catch {
    return { token: null, user: null };
  }
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState(() => loadFromStorage());

  const login = useCallback((result: AuthResult) => {
    const user: AuthUser = {
      userId: result.userId,
      email: result.email,
      firstName: result.firstName,
      lastName: result.lastName,
    };
    localStorage.setItem(TOKEN_KEY, result.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    setState({ token: result.token, user });
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setState({ token: null, user: null });
  }, []);

  return (
    <AuthContext.Provider
      value={{ ...state, isAuthenticated: !!state.token, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>');
  return ctx;
}
