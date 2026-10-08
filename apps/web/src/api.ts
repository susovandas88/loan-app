import type {
  ApiError,
  Application,
  ApplicationStatusDto,
  LoanProduct,
  PagedResult,
  UploadUrlResponse,
  DocumentType,
  ApplicantUser
} from "./types";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5088";
const TOKEN_KEY = "loanapp.accessToken";

let onUnauthorized: (() => void) | null = null;

export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler;
}

export type LoginResult = {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  userId: string;
  email: string;
  name: string;
  role: "Applicant" | "Reviewer" | "SuperAdmin";
};

async function request<T>(path: string, init?: RequestInit & { anonymous?: boolean }): Promise<T> {
  const token = sessionStorage.getItem(TOKEN_KEY);
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...(!init?.anonymous && token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers
    }
  });

  if (!response.ok) {
    if (response.status === 401 && !init?.anonymous) {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem("loanapp.user");
      onUnauthorized?.();
    }
    let error: ApiError = { code: "http_error", message: response.statusText };
    try {
      error = (await response.json()) as ApiError;
    } catch {
      /* keep default */
    }
    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  login: (email: string, password: string) =>
    request<LoginResult>("/api/v1/auth/login", {
      method: "POST",
      anonymous: true,
      body: JSON.stringify({ email, password })
    }),
  products: () => request<LoanProduct[]>("/api/v1/loan-products"),
  listApplicants: () => request<ApplicantUser[]>("/api/v1/users/applicants"),
  listApplications: () => request<PagedResult<Application>>("/api/v1/applications"),
  getApplication: (id: string) => request<Application>(`/api/v1/applications/${id}`),
  getStatus: (id: string) => request<ApplicationStatusDto>(`/api/v1/applications/${id}/status`),
  createApplication: (body: Record<string, unknown>) =>
    request<Application>("/api/v1/applications", { method: "POST", body: JSON.stringify(body) }),
  updateApplication: (id: string, body: Record<string, unknown>) =>
    request<Application>(`/api/v1/applications/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  requestUploadUrl: (id: string, documentType: DocumentType, fileName: string, contentType: string) =>
    request<UploadUrlResponse>(`/api/v1/applications/${id}/documents/upload-url`, {
      method: "POST",
      body: JSON.stringify({ documentType, fileName, contentType })
    }),
  completeUpload: (id: string, documentId: string) =>
    request<Application>(`/api/v1/applications/${id}/documents/${documentId}/complete`, { method: "POST" }),
  submit: (id: string) =>
    request<Application>(`/api/v1/applications/${id}/submit`, {
      method: "POST",
      headers: { "Idempotency-Key": id }
    })
};

export async function uploadFile(uploadUrl: string, file: File): Promise<void> {
  const response = await fetch(uploadUrl, {
    method: "PUT",
    headers: { "Content-Type": file.type || "application/octet-stream" },
    body: file
  });
  if (!response.ok) {
    throw { code: "upload_failed", message: "Could not upload the file." } satisfies ApiError;
  }
}
