using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceWentOfflineDomainEventHandler : INotificationHandler<DeviceWentOfflineDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public DeviceWentOfflineDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public Task Handle(DeviceWentOfflineDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDetailsNotifier.PublishDeviceDetailsChanged(notification.DeviceId, cancellationToken);
    }
}
