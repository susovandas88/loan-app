using System.Text.Json;
using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;
using Microsoft.Extensions.Logging;

namespace LoanApp.Application;

public sealed class ApplicationProcessingPipeline
{
    private readonly IApplicationRepository _repository;
    private readonly IBlobStorageService _blobs;
    private readonly IMalwareScan _scan;
    private readonly IDocumentIntelligenceClient _intelligence;
    private readonly IVerificationEngine _verification;
    private readonly IBankCoreClient _bank;
    private readonly IMessageBus _bus;
    private readonly IClock _clock;
    private readonly ILogger<ApplicationProcessingPipeline> _logger;

    public ApplicationProcessingPipeline(
        IApplicationRepository repository,
        IBlobStorageService blobs,
        IMalwareScan scan,
        IDocumentIntelligenceClient intelligence,
        IVerificationEngine verification,
        IBankCoreClient bank,
        IMessageBus bus,
        IClock clock,
        ILogger<ApplicationProcessingPipeline> logger)
    {
        _repository = repository;
        _blobs = blobs;
        _scan = scan;
        _intelligence = intelligence;
        _verification = verification;
        _bank = bank;
        _bus = bus;
        _clock = clock;
        _logger = logger;
    }

    public async Task ProcessSubmittedAsync(Guid applicationId, CancellationToken ct)
    {
        var app = await _repository.GetByIdAsync(applicationId, ct)
            ?? throw AppException.NotFound("Application not found.");

        if (app.Status is ApplicationStatus.SentToBank or ApplicationStatus.BankAccepted)
        {
            return;
        }

        app.Status = ApplicationStatus.Verifying;
        app.UpdatedAt = _clock.UtcNow;
        await _repository.SaveChangesAsync(ct);

        foreach (var doc in app.Documents.Where(d => d.Lifecycle == DocumentLifecycle.PendingScan).ToList())
        {
            await using var scanStream = await _blobs.OpenReadAsync(doc.BlobPath, ct);
            var clean = await _scan.IsCleanAsync(doc.FileName, scanStream, ct);
            if (!clean)
            {
                doc.Lifecycle = DocumentLifecycle.ScanFailed;
                doc.ScanPassed = false;
                doc.UpdatedAt = _clock.UtcNow;
                continue;
            }

            doc.ScanPassed = true;
            await using var extractStream = await _blobs.OpenReadAsync(doc.BlobPath, ct);
            var facts = await _intelligence.ExtractAsync(doc, extractStream, ct);
            doc.ExtractedJson = JsonSerializer.Serialize(facts);
            doc.ExtractionConfidence = facts.Confidence;
            doc.Lifecycle = DocumentLifecycle.Extracted;
            doc.UpdatedAt = _clock.UtcNow;
            await _bus.PublishAsync(
                IntegrationEvents.DocumentExtracted,
                new { applicationId = app.Id, documentId = doc.Id },
                ct);
        }

        var result = await _verification.VerifyAsync(app, ct);
        app.Findings.Clear();
        foreach (var finding in result.Findings)
        {
            app.Findings.Add(new VerificationFinding
            {
                Id = Guid.NewGuid(),
                ApplicationId = app.Id,
                Code = finding.Code,
                Message = finding.Message,
                Passed = finding.Passed,
                Engine = finding.Engine,
                CreatedAt = _clock.UtcNow
            });
        }

        app.DecisionEngine = result.Engine;
        await _bus.PublishAsync(
            IntegrationEvents.VerificationCompleted,
            new { applicationId = app.Id, passed = result.Passed, engine = result.Engine },
            ct);

        if (!result.Passed)
        {
            app.Status = ApplicationStatus.ActionRequired;
            app.StatusReasonCode = result.ReasonCode;
            app.UpdatedAt = _clock.UtcNow;
            await _repository.SaveChangesAsync(ct);
            return;
        }

        var bank = await _bank.SubmitAsync(app, app.Id.ToString(), ct);
        app.BankReference = bank.Reference;
        app.Status = bank.Accepted ? ApplicationStatus.SentToBank : ApplicationStatus.BankRejected;
        if (bank.Accepted)
        {
            app.Status = ApplicationStatus.BankAccepted;
            app.StatusReasonCode = null;
        }
        else
        {
            app.StatusReasonCode = bank.Reason ?? "bank_rejected";
        }

        app.UpdatedAt = _clock.UtcNow;
        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Processed application {ApplicationId} to {Status}", app.Id, app.Status);
    }
}
