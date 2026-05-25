using MediatR;

namespace Application.Commands.Devices.UpdateDeviceCapabilitiesState;

public sealed record UpdateDeviceCapabilitiesStateCommand(
    Guid DeviceId,
    IEnumerable<DeviceCapabilityStateModel> States
) : IRequest;
