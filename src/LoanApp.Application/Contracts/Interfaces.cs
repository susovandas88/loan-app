using LoanApp.Application.Domain;

namespace LoanApp.Application.Contracts;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUser
{
    string UserId { get; }
    UserRole Role { get; }
}

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> FindByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<AppUser>> ListApplicantsAsync(CancellationToken ct);
}

public interface IFeatureFlags
{
    bool AiAssistant { get; }
    bool AiVerificationAssist { get; }
}

public interface IApplicationRepository
{
    Task<LoanApplication?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<LoanApplication?> GetByIdForUserAsync(Guid id, string userId, CancellationToken ct);
    Task<IReadOnlyList<LoanApplication>> ListForUserAsync(string userId, int page, int pageSize, CancellationToken ct);
    Task<int> CountForUserAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<LoanApplication>> ListAllAsync(int page, int pageSize, CancellationToken ct);
    Task<int> CountAllAsync(CancellationToken ct);
    Task AddAsync(LoanApplication application, CancellationToken ct);
    Task AddDocumentAsync(LoanDocument document, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IBlobStorageService
{
    Task<(string BlobPath, string UploadUrl, DateTimeOffset ExpiresAt)> CreateWriteUrlAsync(
        Guid applicationId,
        Guid documentId,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task<bool> ExistsAsync(string blobPath, CancellationToken ct);

    Task<Stream> OpenReadAsync(string blobPath, CancellationToken ct);
}

public interface IMessageBus
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct);
}

public interface IMalwareScan
{
    Task<bool> IsCleanAsync(string fileName, Stream content, CancellationToken ct);
}

public interface IDocumentIntelligenceClient
{
    Task<ExtractedDocumentFacts> ExtractAsync(LoanDocument document, Stream content, CancellationToken ct);
}

/// <summary>
/// v1: rules + Document Intelligence. Later wrap or replace with Azure OpenAI review.
/// </summary>
public interface IVerificationEngine
{
    Task<VerificationResult> VerifyAsync(LoanApplication application, CancellationToken ct);
}

/// <summary>
/// v1 no-op. Later: grounded Q&amp;A via Azure OpenAI. Never posts to the bank.
/// </summary>
public interface ILoanAssistant
{
    Task<string> ReplyAsync(LoanApplication application, string userMessage, CancellationToken ct);
}

/// <summary>
/// Sole writer to bank core. Models must not invent payloads outside this client.
/// </summary>
public interface IBankCoreClient
{
    Task<BankPostResult> SubmitAsync(LoanApplication application, string idempotencyKey, CancellationToken ct);
}

public interface ILoanCatalog
{
    IReadOnlyList<LoanProductDto> GetProducts();
    LoanProductDto GetRequired(LoanProductCode code);
}

public static class IntegrationEvents
{
    public const string ApplicationSubmitted = "ApplicationSubmitted";
    public const string DocumentExtracted = "DocumentExtracted";
    public const string VerificationCompleted = "VerificationCompleted";
}
