namespace Application.Common.Errors;

public sealed class FloorNotFoundException : NotFoundException
{
    public FloorNotFoundException(Guid floorId)
        : base($"Floor {floorId} not found")
    {
    }
}
