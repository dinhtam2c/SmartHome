using MediatR;

namespace Application.Commands.Devices.AddDevice;

public sealed record AddDeviceCommand(Guid HomeId, Guid? RoomId, string ProvisionCode) : IRequest<Guid>;
