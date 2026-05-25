using Application.Ports.Messages;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceProvisionCodeGeneratedDomainEventHandler : INotificationHandler<DeviceProvisionCodeGeneratedDomainEvent>
{
    private readonly IDeviceProvisioningSender _deviceProvisioningSender;

    public DeviceProvisionCodeGeneratedDomainEventHandler(IDeviceProvisioningSender deviceProvisioningSender)
    {
        _deviceProvisioningSender = deviceProvisioningSender;
    }

    public async Task Handle(DeviceProvisionCodeGeneratedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _deviceProvisioningSender.SendProvisionCode(
            notification.MacAddress,
            notification.ProvisionCode,
            cancellationToken);
    }
}
