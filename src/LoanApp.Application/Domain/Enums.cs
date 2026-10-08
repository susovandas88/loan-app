namespace LoanApp.Application.Domain;

public enum ApplicationStatus
{
    Draft = 0,
    Submitted = 1,
    Verifying = 2,
    ActionRequired = 3,
    SentToBank = 4,
    BankAccepted = 5,
    BankRejected = 6
}

public enum DocumentType
{
    IdProof = 0,
    AddressProof = 1,
    IncomeProof = 2,
    BankStatement = 3
}

public enum DocumentLifecycle
{
    PendingUpload = 0,
    PendingScan = 1,
    ScanFailed = 2,
    Extracted = 3,
    Failed = 4
}

public enum LoanProductCode
{
    Personal = 0,
    Home = 1,
    Auto = 2
}

public enum UserRole
{
    Applicant = 1,
    Reviewer = 2,
    SuperAdmin = 3
}
