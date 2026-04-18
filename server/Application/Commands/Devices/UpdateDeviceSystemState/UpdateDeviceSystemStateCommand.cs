using MediatR;

namespace Application.Commands.Devices.UpdateDeviceSystemState;

public sealed record UpdateDeviceSystemStateCommand(
    Guid DeviceId,
    int Uptime
) : IRequest;
