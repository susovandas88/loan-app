namespace LoanApp.Application.Domain;

public sealed class LoanApplication
{
    public Guid Id { get; set; }
    public string ApplicantUserId { get; set; } = string.Empty;
    public LoanProductCode ProductCode { get; set; }
    public decimal Amount { get; set; }
    public int TenureMonths { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? StatusReasonCode { get; set; }
    public string? BankReference { get; set; }
    public string DecisionEngine { get; set; } = "Rules";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
    public ICollection<VerificationFinding> Findings { get; set; } = new List<VerificationFinding>();
}
