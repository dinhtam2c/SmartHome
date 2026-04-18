using System.Text.Json;
using Application.Common.Realtime;

namespace Infrastructure.Realtime.Sse;

public class SsePublisher : IRealtimePublisher
{
    private readonly ISseHub _sseHub;

    public SsePublisher(ISseHub sseHub)
    {
        _sseHub = sseHub;
    }

    public async Task PublishToDevice(Guid deviceId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(payload);
        await _sseHub.PublishToDevice(deviceId, new SseMessage(eventName, data));
    }

    public Task PublishToHome(Guid homeId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(payload);
        return _sseHub.PublishToHome(homeId, new SseMessage(eventName, data));
    }

    public Task PublishToRoom(Guid roomId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(payload);
        return _sseHub.PublishToRoom(roomId, new SseMessage(eventName, data));
    }
}
