using MediatR;

namespace Application.UseCases.Devices.DeleteDevice;

public sealed record DeleteDeviceCommand(Guid DeviceId) : IRequest;
