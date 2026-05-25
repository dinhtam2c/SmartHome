namespace Application.Common.Errors;

public sealed class FloorDevicePlacementNotFoundException : NotFoundException
{
    public FloorDevicePlacementNotFoundException(Guid id)
        : base($"Floor device placement {id} not found")
    {
    }
}
