using MediatR;

namespace Application.Queries.Devices.GetDeviceDetails;

public sealed record GetDeviceDetailsQuery(Guid DeviceId) : IRequest<DeviceDetailsDto>;
