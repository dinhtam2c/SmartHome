using MediatR;

namespace Application.UseCases.Floors.DeleteFloor;

public sealed record DeleteFloorCommand(Guid HomeId, Guid FloorId) : IRequest;
