namespace Application.Common.Errors;

public sealed class FloorPlanRoomNotFoundException : NotFoundException
{
    public FloorPlanRoomNotFoundException(Guid id)
        : base($"Floor plan room {id} not found")
    {
    }
}
