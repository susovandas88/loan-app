using System.Text.Json;
using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Application;

public sealed class RulesVerificationEngine : IVerificationEngine
{
    private readonly ILoanCatalog _catalog;

    public RulesVerificationEngine(ILoanCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task<VerificationResult> VerifyAsync(LoanApplication application, CancellationToken ct)
    {
        var findings = new List<FindingDto>();
        var required = _catalog.GetRequired(application.ProductCode).RequiredDocuments;

        var complete = required.All(type =>
            application.Documents.Any(d => d.DocumentType == type && d.Lifecycle == DocumentLifecycle.Extracted && d.ScanPassed));
        findings.Add(new FindingDto(
            "completeness",
            complete ? "All required documents are present and scanned." : "One or more required documents are missing or failed scan.",
            complete,
            "Rules"));

        var readable = true;
        var nameMatch = true;
        var dobMatch = true;
        var idOk = true;
        decimal? extractedIncome = null;

        foreach (var doc in application.Documents.Where(d => d.Lifecycle == DocumentLifecycle.Extracted))
        {
            if (string.IsNullOrWhiteSpace(doc.ExtractedJson) || doc.ExtractionConfidence < 0.5)
            {
                readable = false;
                continue;
            }

            var facts = JsonSerializer.Deserialize<ExtractedDocumentFacts>(doc.ExtractedJson);
            if (facts is null || !facts.Readable)
            {
                readable = false;
            }

            if (facts?.IdExpired == true && doc.DocumentType == DocumentType.IdProof)
            {
                idOk = false;
            }

            if (!string.IsNullOrWhiteSpace(facts?.FullName) &&
                !string.Equals(facts.FullName.Trim(), application.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                nameMatch = false;
            }

            if (facts?.DateOfBirth is { } dob && dob != application.DateOfBirth)
            {
                dobMatch = false;
            }

            if (facts?.MonthlyIncome is { } monthlyFromDoc)
            {
                extractedIncome = monthlyFromDoc;
            }
        }

        findings.Add(new FindingDto("quality", readable ? "Documents are readable." : "A document is not readable enough.", readable, "Rules"));
        findings.Add(new FindingDto("id_valid", idOk ? "Identity document is not expired." : "Identity document appears expired.", idOk, "Rules"));
        findings.Add(new FindingDto("name_match", nameMatch ? "Name matches the application." : "Extracted name does not match the application.", nameMatch, "Rules"));
        findings.Add(new FindingDto("dob_match", dobMatch ? "Date of birth matches the application." : "Extracted date of birth does not match.", dobMatch, "Rules"));

        var income = extractedIncome ?? application.MonthlyIncome;
        var affordable = income * 12m * 0.4m >= application.Amount;
        findings.Add(new FindingDto(
            "income_vs_amount",
            affordable
                ? "Declared income supports the requested amount."
                : "Requested amount is high relative to income.",
            affordable,
            "Rules"));

        var passed = findings.All(f => f.Passed);
        var reason = passed ? null : findings.First(f => !f.Passed).Code;
        return Task.FromResult(new VerificationResult(passed, reason, findings, "Rules"));
    }
}
