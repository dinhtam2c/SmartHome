using MediatR;

namespace Application.UseCases.Devices.GetDetails;

public sealed record GetDeviceDetailsQuery(Guid DeviceId) : IRequest<DeviceDetailsDto>;
