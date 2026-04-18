using MediatR;

namespace Application.Queries.Rooms.GetRoomDetails;

public sealed record GetRoomDetailsQuery(Guid HomeId, Guid RoomId) : IRequest<RoomDetailsDto>;
