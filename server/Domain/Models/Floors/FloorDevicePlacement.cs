namespace Domain.Models.Floors;

public class FloorDevicePlacement
{
    public Guid Id { get; private set; }
    public Guid FloorId { get; private set; }
    public Guid DeviceId { get; private set; }
    public float X { get; private set; }
    public float Y { get; private set; }

    private FloorDevicePlacement()
    {
    }

    internal FloorDevicePlacement(Guid floorId, Guid deviceId, float x, float y)
    {
        if (floorId == Guid.Empty)
            throw new InvalidOperationException("FloorId is required.");

        if (deviceId == Guid.Empty)
            throw new InvalidOperationException("DeviceId is required.");

        Id = Guid.NewGuid();
        FloorId = floorId;
        DeviceId = deviceId;
        Move(x, y);
    }

    internal void Move(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
            throw new InvalidOperationException("Coordinates must be finite numbers.");

        X = x;
        Y = y;
    }
}
