using MediatR;

namespace Application.UseCases.Devices.ClaimDevice;

public sealed record ClaimDeviceCommand(Guid HomeId, Guid? RoomId, string ProvisionCode) : IRequest<Guid>;
