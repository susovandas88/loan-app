using System.Text.Json;
using System.Threading.Channels;
using LoanApp.Application.Contracts;

namespace LoanApp.Infrastructure.Messaging;

public sealed record BusEnvelope(string EventName, string PayloadJson);

public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly Channel<BusEnvelope> _channel = Channel.CreateUnbounded<BusEnvelope>();

    public ChannelReader<BusEnvelope> Reader => _channel.Reader;

    public async Task PublishAsync(string eventName, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await _channel.Writer.WriteAsync(new BusEnvelope(eventName, json), ct);
    }
}
