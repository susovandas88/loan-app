using System.Text.Json;
using Azure.Messaging.ServiceBus;
using LoanApp.Application.Contracts;

namespace LoanApp.Infrastructure.Messaging;

public sealed class AzureServiceBusMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusMessageBus(ServiceBusClient client, string queueName)
    {
        _sender = client.CreateSender(queueName);
    }

    public async Task PublishAsync(string eventName, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var message = new ServiceBusMessage(json)
        {
            Subject = eventName,
            ContentType = "application/json"
        };
        await _sender.SendMessageAsync(message, ct);
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
