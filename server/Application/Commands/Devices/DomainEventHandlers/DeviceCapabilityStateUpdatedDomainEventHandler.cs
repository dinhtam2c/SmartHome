using System.Text.Json;
using Application.Common.Data;
using Application.Common.Realtime;
using Core.Domain.Data;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceCapabilityStateUpdatedDomainEventHandler : INotificationHandler<DeviceCapabilityStateUpdatedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public DeviceCapabilityStateUpdatedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public async Task Handle(DeviceCapabilityStateUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDetailsNotifier.PublishDeviceDetailsChanged(notification.DeviceId, cancellationToken);
    }
}
