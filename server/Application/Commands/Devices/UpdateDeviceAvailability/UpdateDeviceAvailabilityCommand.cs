using MediatR;

namespace Application.Commands.Devices.UpdateDeviceAvailability;

public sealed record UpdateDeviceAvailabilityCommand(Guid DeviceId, string State) : IRequest;
