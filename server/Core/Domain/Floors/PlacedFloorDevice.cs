namespace Core.Domain.Floors;

public class PlacedFloorDevice
{
    public Guid Id { get; private set; }
    public Guid FloorId { get; private set; }
    public Guid DeviceId { get; private set; }
    public Guid? FloorRoomId { get; private set; }
    public float X { get; private set; }
    public float Y { get; private set; }

    private PlacedFloorDevice()
    {
    }

    internal PlacedFloorDevice(
        Guid floorId,
        Guid deviceId,
        float x,
        float y,
        Guid? floorRoomId)
    {
        if (floorId == Guid.Empty)
            throw new InvalidOperationException("FloorId is required.");

        if (deviceId == Guid.Empty)
            throw new InvalidOperationException("DeviceId is required.");

        Id = Guid.NewGuid();
        FloorId = floorId;
        DeviceId = deviceId;
        Move(x, y, floorRoomId);
    }

    internal void Move(float x, float y, Guid? floorRoomId)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
            throw new InvalidOperationException("Coordinates must be finite numbers.");

        if (floorRoomId == Guid.Empty)
            throw new InvalidOperationException("FloorRoomId must not be empty.");

        X = x;
        Y = y;
        FloorRoomId = floorRoomId;
    }

    internal void ClearRoomAssignmentIfMatches(Guid floorRoomId)
    {
        if (FloorRoomId == floorRoomId)
        {
            FloorRoomId = null;
        }
    }
}
