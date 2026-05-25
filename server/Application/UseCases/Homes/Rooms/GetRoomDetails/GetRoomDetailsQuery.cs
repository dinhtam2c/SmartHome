using MediatR;

namespace Application.UseCases.Homes.Rooms.GetRoomDetails;

public sealed record GetRoomDetailsQuery(Guid HomeId, Guid RoomId) : IRequest<RoomDetailsDto>;
