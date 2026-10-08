import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { api, uploadFile } from "../api";
import { useAuth } from "../auth";
import type { ApiError, Application, DocumentType } from "../types";

const busy: Application["status"][] = ["Submitted", "Verifying"];

function badgeClass(status: Application["status"]): string {
  if (status === "BankAccepted" || status === "SentToBank") return "badge ok";
  if (status === "ActionRequired" || status === "BankRejected") return "badge bad";
  if (status === "Verifying" || status === "Submitted") return "badge warn";
  return "badge";
}

export default function ApplicationDetail() {
  const { user } = useAuth();
  const { id } = useParams<{ id: string }>();
  const [app, setApp] = useState<Application | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busyUpload, setBusyUpload] = useState<DocumentType | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    const data = await api.getApplication(id);
    setApp(data);
  }, [id]);

  useEffect(() => {
    load().catch((err: ApiError) => setError(err.message));
  }, [load]);

  useEffect(() => {
    if (!id || !app || !busy.includes(app.status)) return;
    const timer = window.setInterval(async () => {
      try {
        const status = await api.getStatus(id);
        if (status.status !== app.status) {
          await load();
        }
      } catch {
        /* keep polling */
      }
    }, 2000);
    return () => window.clearInterval(timer);
  }, [id, app, load]);

  async function onFile(type: DocumentType, file: File | undefined) {
    if (!id || !file || !app) return;
    setBusyUpload(type);
    setError(null);
    try {
      const ticket = await api.requestUploadUrl(id, type, file.name, file.type || "application/octet-stream");
      await uploadFile(ticket.uploadUrl, file);
      const updated = await api.completeUpload(id, ticket.documentId);
      setApp(updated);
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setBusyUpload(null);
    }
  }

  async function submit() {
    if (!id) return;
    setError(null);
    try {
      setApp(await api.submit(id));
    } catch (err) {
      setError((err as ApiError).message);
    }
  }

  if (!app) {
    return <section className="card">{error ? <p className="error">{error}</p> : <p>Loading…</p>}</section>;
  }

  const canEdit = user?.role !== "Reviewer" && (app.status === "Draft" || app.status === "ActionRequired");

  return (
    <section className="card">
      <div className="row">
        <h1>{app.productCode} application</h1>
        <span className={badgeClass(app.status)}>{app.status}</span>
      </div>
      <p className="muted">
        {app.fullName ? `${app.fullName} · ` : ""}
        {app.amount.toLocaleString()} · {app.tenureMonths} months · engine {app.decisionEngine}
      </p>
      {app.statusReasonCode && <p className="error">Action: {app.statusReasonCode}</p>}
      {app.bankReference && <p>Bank reference: {app.bankReference}</p>}
      {error && <p className="error">{error}</p>}

      <h2>Documents</h2>
      <ul className="plain">
        {app.checklist.map((item) => (
          <li key={item.documentType} className="row">
            <div>
              <strong>{item.documentType}</strong>
              <div className="muted">{item.satisfied ? "Uploaded" : "Required"}</div>
            </div>
            {canEdit && (
              <input
                type="file"
                disabled={busyUpload === item.documentType}
                onChange={(event) => void onFile(item.documentType, event.target.files?.[0])}
              />
            )}
          </li>
        ))}
      </ul>

      {app.findings.length > 0 && (
        <>
          <h2>Verification</h2>
          <ul className="plain">
            {app.findings.map((f) => (
              <li key={f.code}>
                <span className={f.passed ? "badge ok" : "badge bad"}>{f.passed ? "Pass" : "Fail"}</span> {f.message}
              </li>
            ))}
          </ul>
        </>
      )}

      {canEdit && (
        <button type="button" onClick={() => void submit()}>
          Submit for verification
        </button>
      )}
    </section>
  );
}
