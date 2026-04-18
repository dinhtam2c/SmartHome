using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceSystemStateUpdatedDomainEventHandler : INotificationHandler<DeviceSystemStateUpdatedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public DeviceSystemStateUpdatedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public Task Handle(DeviceSystemStateUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDetailsNotifier.PublishDeviceDetailsChanged(notification.DeviceId, cancellationToken);
    }
}
