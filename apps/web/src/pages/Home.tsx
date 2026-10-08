import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import type { ApiError, Application } from "../types";

export default function Home() {
  const { user } = useAuth();
  const [items, setItems] = useState<Application[]>([]);
  const [error, setError] = useState<string | null>(null);
  const reviewer = user?.role === "Reviewer";

  useEffect(() => {
    api
      .listApplications()
      .then((page) => setItems(page.items))
      .catch((err: ApiError) => setError(err.message));
  }, []);

  return (
    <section className="card">
      <div className="row">
        <h1>{reviewer ? "Applications" : user?.role === "SuperAdmin" ? "All applications" : "Your applications"}</h1>
        {!reviewer && (
          <Link to="/apply">
            <button type="button">New application</button>
          </Link>
        )}
      </div>
      {error && <p className="error">{error}</p>}
      {items.length === 0 && !error && <p className="muted">No applications yet.</p>}
      <ul className="plain">
        {items.map((app) => (
          <li key={app.id} className="row">
            <div>
              <strong>{app.productCode}</strong> · {app.amount.toLocaleString()} · {app.tenureMonths} months
              {app.fullName && <div className="muted">{app.fullName}</div>}
            </div>
            <Link to={`/applications/${app.id}`}>{app.status}</Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
