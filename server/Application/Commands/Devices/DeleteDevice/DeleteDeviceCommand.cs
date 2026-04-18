using MediatR;

namespace Application.Commands.Devices.DeleteDevice;

public sealed record DeleteDeviceCommand(Guid DeviceId) : IRequest;
