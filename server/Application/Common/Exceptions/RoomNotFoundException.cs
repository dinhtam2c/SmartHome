namespace Application.Exceptions;

public class RoomNotFoundException : NotFoundException
{
    public RoomNotFoundException(Guid roomId)
        : base($"Room {roomId} not found") { }
}
