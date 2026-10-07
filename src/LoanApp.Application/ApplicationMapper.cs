using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Application;

public static class ApplicationMapper
{
    public static ApplicationDto ToDto(LoanApplication app, ILoanCatalog catalog)
    {
        var required = catalog.GetRequired(app.ProductCode).RequiredDocuments;
        var checklist = required.Select(type =>
            new ChecklistItemDto(
                type,
                app.Documents.Any(d =>
                    d.DocumentType == type &&
                    d.Lifecycle is DocumentLifecycle.PendingScan or DocumentLifecycle.Extracted)))
            .ToList();

        return new ApplicationDto(
            app.Id,
            app.ProductCode,
            app.Amount,
            app.TenureMonths,
            app.FullName,
            app.DateOfBirth,
            app.Email,
            app.MonthlyIncome,
            app.Status,
            app.StatusReasonCode,
            app.BankReference,
            app.DecisionEngine,
            app.Documents.Select(d => new DocumentDto(
                d.Id, d.DocumentType, d.Lifecycle, d.ExtractionConfidence, d.ScanPassed)).ToList(),
            checklist,
            app.Findings.Select(f => new FindingDto(f.Code, f.Message, f.Passed, f.Engine)).ToList(),
            app.CreatedAt,
            app.UpdatedAt);
    }
}
