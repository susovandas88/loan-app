namespace LoanApp.Application.Domain;

public sealed class VerificationFinding
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Engine { get; set; } = "Rules";
    public DateTimeOffset CreatedAt { get; set; }
    public LoanApplication Application { get; set; } = null!;
}
