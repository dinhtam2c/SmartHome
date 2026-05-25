using MediatR;

namespace Application.Commands.Floors.DeleteFloor;

public sealed record DeleteFloorCommand(Guid HomeId, Guid FloorId) : IRequest;
