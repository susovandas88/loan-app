using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var expectedKey = builder.Configuration["ApiKey"] ?? "dev-bank-key";
var store = new ConcurrentDictionary<string, BankRecord>();

var app = builder.Build();

app.MapPost("/applications", async (HttpRequest request) =>
{
    if (!request.Headers.TryGetValue("X-Api-Key", out var key) || key != expectedKey)
    {
        return Results.Unauthorized();
    }

    if (!request.Headers.TryGetValue("Idempotency-Key", out var idempotency) || string.IsNullOrWhiteSpace(idempotency))
    {
        return Results.BadRequest(new { accepted = false, reference = "", reason = "missing_idempotency_key" });
    }

    var idem = idempotency.ToString();
    if (store.TryGetValue(idem, out var existing))
    {
        return Results.Ok(new { accepted = existing.Accepted, reference = existing.Reference, reason = existing.Reason });
    }

    var payload = await request.ReadFromJsonAsync<BankApplicationPayload>(new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
    if (payload is null || payload.ApplicationId == Guid.Empty)
    {
        return Results.BadRequest(new { accepted = false, reference = "", reason = "invalid_payload" });
    }

    var accepted = payload.Amount > 0 && payload.Amount < 10_000_000;
    var reference = $"BANK-{payload.ApplicationId:N}".ToUpperInvariant();
    var record = new BankRecord(
        accepted,
        reference.Length > 16 ? reference[..16] : reference,
        accepted ? null : "amount_out_of_policy");
    store[idem] = record;
    return Results.Ok(new { accepted = record.Accepted, reference = record.Reference, reason = record.Reason });
});


app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

internal sealed record BankRecord(bool Accepted, string Reference, string? Reason);

internal sealed record BankApplicationPayload(
    Guid ApplicationId,
    string? Product,
    decimal Amount,
    int TenureMonths);
