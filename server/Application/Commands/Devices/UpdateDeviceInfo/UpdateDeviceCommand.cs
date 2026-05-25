using MediatR;

namespace Application.Commands.Devices.UpdateDeviceInfo;

public sealed record UpdateDeviceInfoCommand(
    Guid DeviceId,
    string Name
) : IRequest;
