using System.Threading.Channels;

namespace Presentation.Realtime.Sse;

internal sealed class SseHub
{
    private readonly SubscriptionStore _homes = new();
    private readonly SubscriptionStore _rooms = new();
    private readonly SubscriptionStore _devices = new();

    public Channel<SseMessage> SubscribeToHome(Guid homeId) => _homes.Subscribe(homeId);

    public Channel<SseMessage> SubscribeToRoom(Guid roomId) => _rooms.Subscribe(roomId);

    public Channel<SseMessage> SubscribeToDevice(Guid deviceId) => _devices.Subscribe(deviceId);

    public void UnsubscribeFromHome(Guid homeId, Channel<SseMessage> channel) =>
        _homes.Unsubscribe(homeId, channel);

    public void UnsubscribeFromRoom(Guid roomId, Channel<SseMessage> channel) =>
        _rooms.Unsubscribe(roomId, channel);

    public void UnsubscribeFromDevice(Guid deviceId, Channel<SseMessage> channel) =>
        _devices.Unsubscribe(deviceId, channel);

    public void PublishToHome(Guid homeId, SseMessage message) => _homes.Publish(homeId, message);

    public void PublishToRoom(Guid roomId, SseMessage message) => _rooms.Publish(roomId, message);

    public void PublishToDevice(Guid deviceId, SseMessage message) => _devices.Publish(deviceId, message);

    private sealed class SubscriptionStore
    {
        private const int ChannelCapacity = 256;

        private readonly object _gate = new();
        private readonly Dictionary<Guid, List<Channel<SseMessage>>> _channelsByKey = [];

        public Channel<SseMessage> Subscribe(Guid key)
        {
            var channel = CreateChannel();

            lock (_gate)
            {
                if (!_channelsByKey.TryGetValue(key, out var channels))
                {
                    channels = [];
                    _channelsByKey.Add(key, channels);
                }

                channels.Add(channel);
            }

            return channel;
        }

        public void Unsubscribe(Guid key, Channel<SseMessage> channel)
        {
            lock (_gate)
            {
                RemoveChannel(key, channel);
            }

            channel.Writer.TryComplete();
        }

        public void Publish(Guid key, SseMessage message)
        {
            List<Channel<SseMessage>> channels;
            lock (_gate)
            {
                if (!_channelsByKey.TryGetValue(key, out var subscribers))
                    return;

                channels = subscribers.ToList();
            }

            var staleChannels = channels
                .Where(channel => !channel.Writer.TryWrite(message))
                .ToList();
            if (staleChannels.Count == 0)
                return;

            lock (_gate)
            {
                foreach (var channel in staleChannels)
                {
                    RemoveChannel(key, channel);
                }
            }

            foreach (var channel in staleChannels)
            {
                channel.Writer.TryComplete();
            }
        }

        private void RemoveChannel(Guid key, Channel<SseMessage> channel)
        {
            if (!_channelsByKey.TryGetValue(key, out var channels))
                return;

            channels.Remove(channel);
            if (channels.Count == 0)
                _channelsByKey.Remove(key);
        }

        private static Channel<SseMessage> CreateChannel()
        {
            return Channel.CreateBounded<SseMessage>(
                new BoundedChannelOptions(ChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });
        }
    }
}
