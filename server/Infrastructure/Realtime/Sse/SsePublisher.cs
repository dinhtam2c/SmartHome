using System.Text.Json;
using Application.Common.Realtime;

namespace Infrastructure.Realtime.Sse;

public class SsePublisher : IRealtimePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISseHub _sseHub;

    public SsePublisher(ISseHub sseHub)
    {
        _sseHub = sseHub;
    }

    public async Task PublishToDevice(
        Guid deviceId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(delta, JsonOptions);
        await _sseHub.PublishToDevice(deviceId, CreateMessage(data));
    }

    public Task PublishToHome(
        Guid homeId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(delta, JsonOptions);
        return _sseHub.PublishToHome(homeId, CreateMessage(data));
    }

    public Task PublishToRoom(
        Guid roomId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(delta, JsonOptions);
        return _sseHub.PublishToRoom(roomId, CreateMessage(data));
    }

    private static SseMessage CreateMessage(string data)
    {
        return new SseMessage(
            RealtimeEventNames.RealtimeDelta,
            data,
            Guid.NewGuid().ToString("N"),
            RetryMs: 3000);
    }
}
