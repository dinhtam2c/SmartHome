using MediatR;

namespace Application.UseCases.Devices.UpdateInfo;

public sealed record UpdateDeviceInfoCommand(
    Guid DeviceId,
    string Name
) : IRequest;
