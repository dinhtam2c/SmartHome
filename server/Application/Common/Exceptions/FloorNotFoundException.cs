namespace Application.Exceptions;

public sealed class FloorNotFoundException : NotFoundException
{
    public FloorNotFoundException(Guid floorId)
        : base($"Floor {floorId} not found")
    {
    }
}
