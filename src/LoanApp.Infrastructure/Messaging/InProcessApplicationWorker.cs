using System.Text.Json;
using LoanApp.Application;
using LoanApp.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoanApp.Infrastructure.Messaging;

public sealed class InProcessApplicationWorker : BackgroundService
{
    private readonly InMemoryMessageBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InProcessApplicationWorker> _logger;

    public InProcessApplicationWorker(
        InMemoryMessageBus bus,
        IServiceScopeFactory scopeFactory,
        ILogger<InProcessApplicationWorker> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in _bus.Reader.ReadAllAsync(stoppingToken))
        {
            if (envelope.EventName != IntegrationEvents.ApplicationSubmitted)
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(envelope.PayloadJson);
                var id = doc.RootElement.GetProperty("applicationId").GetGuid();
                await using var scope = _scopeFactory.CreateAsyncScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<ApplicationProcessingPipeline>();
                await pipeline.ProcessSubmittedAsync(id, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing in-memory bus message {Event}", envelope.EventName);
            }
        }
    }
}
