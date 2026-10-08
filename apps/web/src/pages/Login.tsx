import { FormEvent, useState } from "react";
import { useAuth } from "../auth";
import type { ApiError } from "../types";

export default function Login() {
  const { login } = useAuth();
  const [email, setEmail] = useState("applicant@loan.local");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setPending(true);
    setError(null);
    try {
      await login(email, password);
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Sign-in failed.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="card">
      <h1>Sign in</h1>
      <p className="muted">Applicant, reviewer, and super admin each sign in with their own account.</p>
      <form onSubmit={onSubmit}>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <label htmlFor="password">Password</label>
        <input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={pending}>
          {pending ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </section>
  );
}
