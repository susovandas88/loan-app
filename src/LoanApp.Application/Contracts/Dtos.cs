using LoanApp.Application.Domain;

namespace LoanApp.Application.Contracts;

public sealed record ApiError(string Code, string Message, object? Details = null);

public sealed record CreateApplicationRequest(
    LoanProductCode ProductCode,
    decimal Amount,
    int TenureMonths,
    string FullName,
    DateOnly DateOfBirth,
    string Email,
    decimal MonthlyIncome);

public sealed record UpdateApplicationRequest(
    decimal Amount,
    int TenureMonths,
    string FullName,
    DateOnly DateOfBirth,
    string Email,
    decimal MonthlyIncome);

public sealed record UploadUrlRequest(DocumentType DocumentType, string FileName, string ContentType);

public sealed record UploadUrlResponse(Guid DocumentId, string UploadUrl, DateTimeOffset ExpiresAt);

public sealed record DocumentDto(
    Guid Id,
    DocumentType DocumentType,
    DocumentLifecycle Lifecycle,
    double ExtractionConfidence,
    bool ScanPassed);

public sealed record ChecklistItemDto(DocumentType DocumentType, bool Satisfied);

public sealed record FindingDto(string Code, string Message, bool Passed, string Engine);

public sealed record ApplicationDto(
    Guid Id,
    LoanProductCode ProductCode,
    decimal Amount,
    int TenureMonths,
    string FullName,
    DateOnly DateOfBirth,
    string Email,
    decimal MonthlyIncome,
    ApplicationStatus Status,
    string? StatusReasonCode,
    string? BankReference,
    string DecisionEngine,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<ChecklistItemDto> Checklist,
    IReadOnlyList<FindingDto> Findings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ApplicationStatusDto(
    Guid Id,
    ApplicationStatus Status,
    string? StatusReasonCode,
    string DecisionEngine);

public sealed record LoanProductDto(
    LoanProductCode Code,
    string Name,
    IReadOnlyList<DocumentType> RequiredDocuments);

public sealed record ExtractedDocumentFacts(
    string? FullName,
    DateOnly? DateOfBirth,
    decimal? MonthlyIncome,
    bool Readable,
    bool IdExpired,
    double Confidence);

public sealed record VerificationResult(
    bool Passed,
    string? ReasonCode,
    IReadOnlyList<FindingDto> Findings,
    string Engine);

public sealed record BankPostResult(bool Accepted, string Reference, string? Reason);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
