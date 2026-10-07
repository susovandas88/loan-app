using System.Text.Json;
using LoanApp.Application;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LoanApp.Functions;

public sealed class ApplicationSubmittedFunction
{
    private readonly ApplicationProcessingPipeline _pipeline;
    private readonly ILogger<ApplicationSubmittedFunction> _logger;

    public ApplicationSubmittedFunction(
        ApplicationProcessingPipeline pipeline,
        ILogger<ApplicationSubmittedFunction> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    [Function(nameof(OnApplicationSubmitted))]
    public async Task OnApplicationSubmitted(
        [ServiceBusTrigger("%ServiceBusQueue%", Connection = "ServiceBusConnection")] string message,
        FunctionContext context)
    {
        _ = context;
        using var doc = JsonDocument.Parse(message);
        var id = doc.RootElement.GetProperty("applicationId").GetGuid();
        _logger.LogInformation("Processing submitted application {ApplicationId}", id);
        await _pipeline.ProcessSubmittedAsync(id, CancellationToken.None);
    }
}
