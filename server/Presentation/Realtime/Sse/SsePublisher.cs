using System.Text.Json;
using Application.Common.Realtime;
using Application.Ports.Realtime;

namespace Presentation.Realtime.Sse;

internal sealed class SsePublisher : IRealtimePublisher
{
    private const string RealtimeDeltaEvent = "RealtimeDelta";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SseHub _sseHub;

    public SsePublisher(SseHub sseHub)
    {
        _sseHub = sseHub;
    }

    public Task PublishToDevice(
        Guid deviceId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sseHub.PublishToDevice(deviceId, CreateMessage(delta));
        return Task.CompletedTask;
    }

    public Task PublishToHome(
        Guid homeId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sseHub.PublishToHome(homeId, CreateMessage(delta));
        return Task.CompletedTask;
    }

    public Task PublishToRoom(
        Guid roomId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sseHub.PublishToRoom(roomId, CreateMessage(delta));
        return Task.CompletedTask;
    }

    private static SseMessage CreateMessage(RealtimeDelta delta)
    {
        return new SseMessage(
            RealtimeDeltaEvent,
            JsonSerializer.Serialize(delta, JsonOptions),
            Guid.NewGuid().ToString("N"),
            RetryMs: 3000);
    }
}
