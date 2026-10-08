import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, setUnauthorizedHandler, type LoginResult } from "./api";

const TOKEN_KEY = "loanapp.accessToken";
const USER_KEY = "loanapp.user";

export type SessionUser = {
  userId: string;
  email: string;
  name: string;
  role: "Applicant" | "Reviewer" | "SuperAdmin";
};

type AuthState = {
  token: string | null;
  user: SessionUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

function readUser(): SessionUser | null {
  const raw = sessionStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as SessionUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => sessionStorage.getItem(TOKEN_KEY));
  const [user, setUser] = useState<SessionUser | null>(() => readUser());

  const logout = () => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    setToken(null);
    setUser(null);
  };

  useEffect(() => setUnauthorizedHandler(logout), []);

  const login = async (email: string, password: string) => {
    const result: LoginResult = await api.login(email, password);
    const session = { userId: result.userId, email: result.email, name: result.name, role: result.role };
    sessionStorage.setItem(TOKEN_KEY, result.accessToken);
    sessionStorage.setItem(USER_KEY, JSON.stringify(session));
    setToken(result.accessToken);
    setUser(session);
  };

  return <AuthContext.Provider value={{ token, user, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("AuthProvider is missing.");
  }
  return value;
}
