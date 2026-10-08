import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api";
import { useAuth } from "../auth";
import type { ApiError, ApplicantUser, LoanProduct } from "../types";

export default function NewApplication() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [products, setProducts] = useState<LoanProduct[]>([]);
  const [applicants, setApplicants] = useState<ApplicantUser[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const admin = user?.role === "SuperAdmin";

  useEffect(() => {
    api.products().then(setProducts).catch((err: ApiError) => setError(err.message));
    if (admin) {
      api.listApplicants().then(setApplicants).catch((err: ApiError) => setError(err.message));
    }
  }, [admin]);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setSaving(true);
    setError(null);
    try {
      const created = await api.createApplication({
        productCode: form.get("productCode"),
        amount: Number(form.get("amount")),
        tenureMonths: Number(form.get("tenureMonths")),
        fullName: form.get("fullName"),
        dateOfBirth: form.get("dateOfBirth"),
        email: form.get("email"),
        monthlyIncome: Number(form.get("monthlyIncome")),
        applicantUserId: admin ? form.get("applicantUserId") : undefined
      });
      navigate(`/applications/${created.id}`);
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="card">
      <h1>{admin ? "Apply on behalf of an applicant" : "Apply for a loan"}</h1>
      {error && <p className="error">{error}</p>}
      <form onSubmit={onSubmit}>
        <div className="grid">
          <div>
            <label htmlFor="productCode">Product</label>
            <select id="productCode" name="productCode" required>
              {products.map((p) => (
                <option key={p.code} value={p.code}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="amount">Amount</label>
            <input id="amount" name="amount" type="number" min={1000} step="100" defaultValue={50000} required />
          </div>
          <div>
            <label htmlFor="tenureMonths">Tenure (months)</label>
            <input id="tenureMonths" name="tenureMonths" type="number" min={6} max={360} defaultValue={36} required />
          </div>
          {admin && (
            <div>
              <label htmlFor="applicantUserId">Applicant</label>
              <select id="applicantUserId" name="applicantUserId" required>
                <option value="">Select applicant</option>
                {applicants.map((person) => (
                  <option key={person.id} value={person.id}>
                    {person.displayName} ({person.email})
                  </option>
                ))}
              </select>
            </div>
          )}
          <div>
            <label htmlFor="fullName">Full name</label>
            <input id="fullName" name="fullName" required />
          </div>
          <div>
            <label htmlFor="dateOfBirth">Date of birth</label>
            <input id="dateOfBirth" name="dateOfBirth" type="date" required />
          </div>
          <div>
            <label htmlFor="email">Email</label>
            <input id="email" name="email" type="email" required />
          </div>
          <div>
            <label htmlFor="monthlyIncome">Monthly income</label>
            <input id="monthlyIncome" name="monthlyIncome" type="number" min={1} defaultValue={8000} required />
          </div>
        </div>
        <button type="submit" disabled={saving}>
          {saving ? "Saving…" : "Create draft"}
        </button>
      </form>
    </section>
  );
}
