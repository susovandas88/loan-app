using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Infrastructure.Intelligence;

public sealed class FileNameMalwareScan : IMalwareScan
{
    public Task<bool> IsCleanAsync(string fileName, Stream content, CancellationToken ct)
    {
        _ = content;
        var name = fileName.ToLowerInvariant();
        var dirty = name.EndsWith(".exe") || name.Contains("virus") || name.Contains("eicar");
        return Task.FromResult(!dirty);
    }
}

public sealed class StubDocumentIntelligenceClient : IDocumentIntelligenceClient
{
    public async Task<ExtractedDocumentFacts> ExtractAsync(LoanDocument document, Stream content, CancellationToken ct)
    {
        _ = content;
        await Task.Yield();
        var name = document.FileName.ToLowerInvariant();
        var unreadable = name.Contains("unreadable") || name.Contains("blur");
        var expired = name.Contains("expired");
        return new ExtractedDocumentFacts(
            FullName: null,
            DateOfBirth: null,
            MonthlyIncome: null,
            Readable: !unreadable,
            IdExpired: expired && document.DocumentType == DocumentType.IdProof,
            Confidence: unreadable ? 0.2 : 0.92);
    }
}
