using Application.Common.Message;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceProvisionCodeGeneratedDomainEventHandler : INotificationHandler<DeviceProvisionCodeGeneratedDomainEvent>
{
    private readonly IDeviceMessagePublisher _deviceMessagePublisher;

    public DeviceProvisionCodeGeneratedDomainEventHandler(IDeviceMessagePublisher deviceMessagePublisher)
    {
        _deviceMessagePublisher = deviceMessagePublisher;
    }

    public async Task Handle(DeviceProvisionCodeGeneratedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _deviceMessagePublisher
            .SendProvisionCode(notification.MacAddress, notification.ProvisionCode);
    }
}
