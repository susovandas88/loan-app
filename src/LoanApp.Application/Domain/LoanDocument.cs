namespace LoanApp.Application.Domain;

public sealed class LoanDocument
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = string.Empty;
    public DocumentLifecycle Lifecycle { get; set; } = DocumentLifecycle.PendingUpload;
    public bool ScanPassed { get; set; }
    public string? ExtractedJson { get; set; }
    public double ExtractionConfidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public LoanApplication Application { get; set; } = null!;
}
