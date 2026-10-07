using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Application;

public sealed class LoanApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly IBlobStorageService _blobs;
    private readonly IMessageBus _bus;
    private readonly ILoanCatalog _catalog;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public LoanApplicationService(
        IApplicationRepository repository,
        IBlobStorageService blobs,
        IMessageBus bus,
        ILoanCatalog catalog,
        ICurrentUser user,
        IClock clock)
    {
        _repository = repository;
        _blobs = blobs;
        _bus = bus;
        _catalog = catalog;
        _user = user;
        _clock = clock;
    }

    public IReadOnlyList<LoanProductDto> GetProducts() => _catalog.GetProducts();

    public async Task<ApplicationDto> CreateAsync(CreateApplicationRequest request, CancellationToken ct)
    {
        ValidateProfile(request.Amount, request.TenureMonths, request.FullName, request.Email, request.MonthlyIncome);
        _ = _catalog.GetRequired(request.ProductCode);

        var now = _clock.UtcNow;
        var app = new LoanApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = _user.UserId,
            ProductCode = request.ProductCode,
            Amount = request.Amount,
            TenureMonths = request.TenureMonths,
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Email = request.Email.Trim(),
            MonthlyIncome = request.MonthlyIncome,
            Status = ApplicationStatus.Draft,
            DecisionEngine = "Rules",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(app, ct);
        await _repository.SaveChangesAsync(ct);
        return ApplicationMapper.ToDto(app, _catalog);
    }

    public async Task<ApplicationDto> GetAsync(Guid id, CancellationToken ct)
    {
        var app = await RequireForUser(id, ct);
        return ApplicationMapper.ToDto(app, _catalog);
    }

    public async Task<PagedResult<ApplicationDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = await _repository.ListForUserAsync(_user.UserId, page, pageSize, ct);
        var total = await _repository.CountForUserAsync(_user.UserId, ct);
        return new PagedResult<ApplicationDto>(
            items.Select(a => ApplicationMapper.ToDto(a, _catalog)).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<ApplicationDto> UpdateDraftAsync(Guid id, UpdateApplicationRequest request, CancellationToken ct)
    {
        ValidateProfile(request.Amount, request.TenureMonths, request.FullName, request.Email, request.MonthlyIncome);
        var app = await RequireForUser(id, ct);
        EnsureMutableForApplicant(app);

        app.Amount = request.Amount;
        app.TenureMonths = request.TenureMonths;
        app.FullName = request.FullName.Trim();
        app.DateOfBirth = request.DateOfBirth;
        app.Email = request.Email.Trim();
        app.MonthlyIncome = request.MonthlyIncome;
        app.UpdatedAt = _clock.UtcNow;
        await _repository.SaveChangesAsync(ct);
        return ApplicationMapper.ToDto(app, _catalog);
    }

    public async Task<UploadUrlResponse> CreateUploadUrlAsync(Guid applicationId, UploadUrlRequest request, CancellationToken ct)
    {
        var app = await RequireForUser(applicationId, ct);
        EnsureMutableForApplicant(app);

        var required = _catalog.GetRequired(app.ProductCode).RequiredDocuments;
        if (!required.Contains(request.DocumentType))
        {
            throw new AppException("invalid_document_type", "This document type is not required for the selected product.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new AppException("invalid_file", "File name is required.");
        }

        var now = _clock.UtcNow;
        var existing = app.Documents.FirstOrDefault(d => d.Id != Guid.Empty && d.DocumentType == request.DocumentType);
        if (existing is null)
        {
            existing = new LoanDocument
            {
                Id = Guid.NewGuid(),
                ApplicationId = app.Id,
                DocumentType = request.DocumentType,
                CreatedAt = now,
                UpdatedAt = now
            };
            app.Documents.Add(existing);
            await _repository.AddDocumentAsync(existing, ct);
        }

        existing.FileName = Path.GetFileName(request.FileName);
        existing.ContentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? "application/octet-stream"
            : request.ContentType;
        existing.Lifecycle = DocumentLifecycle.PendingUpload;
        existing.ScanPassed = false;
        existing.ExtractedJson = null;
        existing.ExtractionConfidence = 0;
        existing.UpdatedAt = now;

        var (path, url, expires) = await _blobs.CreateWriteUrlAsync(
            app.Id, existing.Id, existing.FileName, existing.ContentType, ct);
        existing.BlobPath = path;
        app.UpdatedAt = now;
        await _repository.SaveChangesAsync(ct);
        return new UploadUrlResponse(existing.Id, url, expires);
    }

    public async Task<ApplicationDto> CompleteUploadAsync(Guid applicationId, Guid documentId, CancellationToken ct)
    {
        var app = await RequireForUser(applicationId, ct);
        EnsureMutableForApplicant(app);
        var doc = app.Documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw AppException.NotFound("Document not found.");

        if (!await _blobs.ExistsAsync(doc.BlobPath, ct))
        {
            throw new AppException("upload_missing", "No file was found at the upload location.");
        }

        doc.Lifecycle = DocumentLifecycle.PendingScan;
        doc.UpdatedAt = _clock.UtcNow;
        app.UpdatedAt = doc.UpdatedAt;
        await _repository.SaveChangesAsync(ct);
        return ApplicationMapper.ToDto(app, _catalog);
    }

    public async Task<ApplicationDto> SubmitAsync(Guid id, string? idempotencyKey, CancellationToken ct)
    {
        var app = await RequireForUser(id, ct);
        if (app.Status is ApplicationStatus.Submitted or ApplicationStatus.Verifying or ApplicationStatus.SentToBank
            or ApplicationStatus.BankAccepted)
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return ApplicationMapper.ToDto(app, _catalog);
            }

            throw AppException.Conflict("already_submitted", "This application is already in process.");
        }

        if (app.Status is not (ApplicationStatus.Draft or ApplicationStatus.ActionRequired))
        {
            throw AppException.Conflict("invalid_state", "This application cannot be submitted in its current status.");
        }

        var required = _catalog.GetRequired(app.ProductCode).RequiredDocuments;
        var missing = required.Where(type =>
            !app.Documents.Any(d => d.DocumentType == type && d.Lifecycle == DocumentLifecycle.PendingScan))
            .ToList();
        if (missing.Count > 0)
        {
            throw new AppException(
                "incomplete_documents",
                "Upload every required document before submitting.",
                400,
                missing);
        }

        app.Status = ApplicationStatus.Submitted;
        app.StatusReasonCode = null;
        app.UpdatedAt = _clock.UtcNow;
        await _repository.SaveChangesAsync(ct);
        await _bus.PublishAsync(IntegrationEvents.ApplicationSubmitted, new { applicationId = app.Id }, ct);
        return ApplicationMapper.ToDto(app, _catalog);
    }

    public async Task<ApplicationStatusDto> GetStatusAsync(Guid id, CancellationToken ct)
    {
        var app = await RequireForUser(id, ct);
        return new ApplicationStatusDto(app.Id, app.Status, app.StatusReasonCode, app.DecisionEngine);
    }

    private async Task<LoanApplication> RequireForUser(Guid id, CancellationToken ct)
    {
        var app = await _repository.GetByIdForUserAsync(id, _user.UserId, ct);
        return app ?? throw AppException.NotFound("Application not found.");
    }

    private static void EnsureMutableForApplicant(LoanApplication app)
    {
        if (app.Status is ApplicationStatus.Draft or ApplicationStatus.ActionRequired)
        {
            return;
        }

        throw AppException.Conflict("locked", "Documents can only be changed while the application is a draft or action is required.");
    }

    private static void ValidateProfile(decimal amount, int tenure, string name, string email, decimal income)
    {
        if (amount < 1000)
        {
            throw new AppException("invalid_amount", "Loan amount must be at least 1000.");
        }

        if (tenure is < 6 or > 360)
        {
            throw new AppException("invalid_tenure", "Tenure must be between 6 and 360 months.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
        {
            throw new AppException("invalid_name", "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new AppException("invalid_email", "A valid email is required.");
        }

        if (income <= 0)
        {
            throw new AppException("invalid_income", "Monthly income must be greater than zero.");
        }
    }
}
