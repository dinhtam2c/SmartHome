using MediatR;

namespace Application.Queries.Homes.GetHomeDevices;

public sealed record GetHomeDevicesQuery(
    Guid HomeId,
    Guid? RoomId
) : IRequest<IReadOnlyList<HomeDeviceDto>>;
