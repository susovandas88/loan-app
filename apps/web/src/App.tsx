import { Link, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth";
import ApplicationDetail from "./pages/ApplicationDetail";
import Home from "./pages/Home";
import Login from "./pages/Login";
import NewApplication from "./pages/NewApplication";

function Shell() {
  const { token, user, logout } = useAuth();
  if (!token) {
    return (
      <main>
        <Login />
      </main>
    );
  }

  return (
    <>
      <header>
        <Link to="/">Loan Portal</Link>
        <span className="header-user">
          {user?.name ?? user?.email}
          {user?.role ? ` · ${user.role}` : ""}
        </span>
        <button type="button" className="header-button" onClick={logout}>
          Sign out
        </button>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/apply" element={user?.role === "Reviewer" ? <Navigate to="/" replace /> : <NewApplication />} />
          <Route path="/applications/:id" element={<ApplicationDetail />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <Shell />
    </AuthProvider>
  );
}
