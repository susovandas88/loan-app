export type LoanProductCode = "Personal" | "Home" | "Auto";
export type DocumentType = "IdProof" | "AddressProof" | "IncomeProof" | "BankStatement";
export type ApplicationStatus =
  | "Draft"
  | "Submitted"
  | "Verifying"
  | "ActionRequired"
  | "SentToBank"
  | "BankAccepted"
  | "BankRejected";
export type DocumentLifecycle =
  | "PendingUpload"
  | "PendingScan"
  | "ScanFailed"
  | "Extracted"
  | "Failed";

export interface LoanProduct {
  code: LoanProductCode;
  name: string;
  requiredDocuments: DocumentType[];
}

export interface DocumentDto {
  id: string;
  documentType: DocumentType;
  lifecycle: DocumentLifecycle;
  extractionConfidence: number;
  scanPassed: boolean;
}

export interface ChecklistItem {
  documentType: DocumentType;
  satisfied: boolean;
}

export interface Finding {
  code: string;
  message: string;
  passed: boolean;
  engine: string;
}

export interface Application {
  id: string;
  productCode: LoanProductCode;
  amount: number;
  tenureMonths: number;
  fullName: string;
  dateOfBirth: string;
  email: string;
  monthlyIncome: number;
  status: ApplicationStatus;
  statusReasonCode: string | null;
  bankReference: string | null;
  decisionEngine: string;
  documents: DocumentDto[];
  checklist: ChecklistItem[];
  findings: Finding[];
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ApplicationStatusDto {
  id: string;
  status: ApplicationStatus;
  statusReasonCode: string | null;
  decisionEngine: string;
}

export interface UploadUrlResponse {
  documentId: string;
  uploadUrl: string;
  expiresAt: string;
}

export interface ApiError {
  code: string;
  message: string;
  details?: unknown;
}
