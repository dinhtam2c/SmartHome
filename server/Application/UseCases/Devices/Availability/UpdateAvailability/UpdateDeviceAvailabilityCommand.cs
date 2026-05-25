using MediatR;

namespace Application.UseCases.Devices.Availability.UpdateAvailability;

public sealed record UpdateDeviceAvailabilityCommand(Guid DeviceId, string State) : IRequest;
