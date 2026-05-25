using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Application.Ports.Messages;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceProvisionedDomainEventHandler : INotificationHandler<DeviceProvisionedDomainEvent>
{
    private readonly IDeviceProvisioningSender _deviceProvisioningSender;
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceProvisionedDomainEventHandler(
        IDeviceProvisioningSender deviceProvisioningSender,
        IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _deviceProvisioningSender = deviceProvisioningSender;
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(DeviceProvisionedDomainEvent notification, CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            _deviceProvisioningSender.SendCredentials(
                notification.MacAddress,
                notification.DeviceId,
                notification.AccessToken,
                cancellationToken),
            _realtimeDeltaNotifier.PublishDelta(
                notification.DeviceId,
                notification.HomeId,
                notification.RoomId,
                RealtimeChanges.Created,
                new
                {
                    notification.HomeId,
                    notification.RoomId
                },
                cancellationToken: cancellationToken));
    }
}
