using System.Net.Http.Json;
using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Infrastructure.Banking;

public sealed class HttpBankCoreClient : IBankCoreClient
{
    private readonly HttpClient _http;

    public HttpBankCoreClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<BankPostResult> SubmitAsync(LoanApplication application, string idempotencyKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "applications")
        {
            Content = JsonContent.Create(new
            {
                applicationId = application.Id,
                product = application.ProductCode.ToString(),
                amount = application.Amount,
                tenureMonths = application.TenureMonths,
                applicant = new
                {
                    fullName = application.FullName,
                    email = application.Email,
                    monthlyIncome = application.MonthlyIncome
                },
                verificationEngine = application.DecisionEngine,
                extracted = application.Documents.Select(d => new
                {
                    d.DocumentType,
                    d.ExtractionConfidence
                })
            })
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new BankPostResult(false, string.Empty, "bank_http_error");
        }

        var body = await response.Content.ReadFromJsonAsync<SimulatorResponse>(ct);
        if (body is null)
        {
            return new BankPostResult(false, string.Empty, "bank_invalid_response");
        }

        return new BankPostResult(body.Accepted, body.Reference, body.Reason);
    }

    private sealed record SimulatorResponse(bool Accepted, string Reference, string? Reason);
}
