using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Infrastructure.Realtime.Sse;

public class InMemorySseHub : ISseHub
{
    private readonly ConcurrentDictionary<Guid, List<Channel<SseMessage>>> _homeChannels = [];
    private readonly ConcurrentDictionary<Guid, List<Channel<SseMessage>>> _roomChannels = [];
    private readonly ConcurrentDictionary<Guid, List<Channel<SseMessage>>> _deviceChannels = [];

    public Channel<SseMessage> SubscribeToHome(Guid homeId)
    {
        var channel = Channel.CreateUnbounded<SseMessage>();

        AddChannel(_homeChannels, homeId, channel);
        return channel;
    }

    public Channel<SseMessage> SubscribeToRoom(Guid roomId)
    {
        var channel = Channel.CreateUnbounded<SseMessage>();

        AddChannel(_roomChannels, roomId, channel);
        return channel;
    }

    public Channel<SseMessage> SubscribeToDevice(Guid deviceId)
    {
        var channel = Channel.CreateUnbounded<SseMessage>();

        AddChannel(_deviceChannels, deviceId, channel);
        return channel;
    }

    public void UnsubscribeFromHome(Guid homeId, Channel<SseMessage> channel)
    {
        RemoveChannel(_homeChannels, homeId, channel);
    }

    public void UnsubscribeFromRoom(Guid roomId, Channel<SseMessage> channel)
    {
        RemoveChannel(_roomChannels, roomId, channel);
    }

    public void UnsubscribeFromDevice(Guid deviceId, Channel<SseMessage> channel)
    {
        RemoveChannel(_deviceChannels, deviceId, channel);
    }

    public Task PublishToHome(Guid homeId, SseMessage message)
    {
        Publish(_homeChannels, homeId, message);
        return Task.CompletedTask;
    }

    public Task PublishToRoom(Guid roomId, SseMessage message)
    {
        Publish(_roomChannels, roomId, message);
        return Task.CompletedTask;
    }

    public Task PublishToDevice(Guid deviceId, SseMessage message)
    {
        Publish(_deviceChannels, deviceId, message);
        return Task.CompletedTask;
    }

    private static void AddChannel(
        ConcurrentDictionary<Guid, List<Channel<SseMessage>>> map,
        Guid key,
        Channel<SseMessage> channel)
    {
        var list = map.GetOrAdd(key, _ => []);

        lock (list)
        {
            list.Add(channel);
        }
    }

    private static void Publish(
        ConcurrentDictionary<Guid, List<Channel<SseMessage>>> map,
        Guid key,
        SseMessage message)
    {
        if (!map.TryGetValue(key, out var channels))
        {
            return;
        }

        List<Channel<SseMessage>> snapshot;

        lock (channels)
        {
            snapshot = channels.ToList();
        }

        List<Channel<SseMessage>> staleChannels = [];
        foreach (var channel in snapshot)
        {
            if (!channel.Writer.TryWrite(message))
            {
                staleChannels.Add(channel);
            }
        }

        if (staleChannels.Count == 0)
            return;

        lock (channels)
        {
            foreach (var channel in staleChannels)
            {
                channels.Remove(channel);
                channel.Writer.TryComplete();
            }

            if (channels.Count == 0)
            {
                map.TryRemove(key, out _);
            }
        }
    }

    private static void RemoveChannel(
        ConcurrentDictionary<Guid, List<Channel<SseMessage>>> map,
        Guid key,
        Channel<SseMessage> channel)
    {
        if (!map.TryGetValue(key, out var channels))
            return;

        lock (channels)
        {
            channels.Remove(channel);

            if (channels.Count == 0)
            {
                map.TryRemove(key, out _);
            }
        }

        channel.Writer.TryComplete();
    }
}
