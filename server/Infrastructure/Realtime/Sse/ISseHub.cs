using System.Threading.Channels;

namespace Infrastructure.Realtime.Sse;

public interface ISseHub
{
    Channel<SseMessage> SubscribeToHome(Guid homeId);
    Channel<SseMessage> SubscribeToRoom(Guid roomId);
    Channel<SseMessage> SubscribeToDevice(Guid deviceId);

    void UnsubscribeFromHome(Guid homeId, Channel<SseMessage> channel);
    void UnsubscribeFromRoom(Guid roomId, Channel<SseMessage> channel);
    void UnsubscribeFromDevice(Guid deviceId, Channel<SseMessage> channel);

    Task PublishToHome(Guid homeId, SseMessage message);
    Task PublishToRoom(Guid roomId, SseMessage message);
    Task PublishToDevice(Guid deviceId, SseMessage message);
}
