using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceCapabilityStateUpdatedDomainEventHandler : INotificationHandler<DeviceCapabilityStateUpdatedDomainEvent>
{
    private readonly IRealtimePublisher _realtimePublisher;

    public DeviceCapabilityStateUpdatedDomainEventHandler(IRealtimePublisher realtimePublisher)
    {
        _realtimePublisher = realtimePublisher;
    }

    public async Task Handle(DeviceCapabilityStateUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var delta = RealtimeDelta.Create(
            entity: RealtimeEntities.DeviceCapability,
            change: RealtimeChanges.StateChanged,
            homeId: notification.HomeId,
            roomId: notification.RoomId,
            deviceId: notification.DeviceId,
            endpointId: notification.EndpointId,
            capabilityId: notification.CapabilityId,
            delta: new
            {
                notification.ReportedAt,
                notification.State
            });

        await _realtimePublisher.PublishToDevice(notification.DeviceId, delta, cancellationToken);

        if (notification.RoomId.HasValue)
        {
            await _realtimePublisher.PublishToRoom(notification.RoomId.Value, delta, cancellationToken);
        }

        if (notification.HomeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(notification.HomeId.Value, delta, cancellationToken);
        }
    }
}
