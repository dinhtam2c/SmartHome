namespace Core.Entities;

public class SensorData
{
    public Guid Id { get; set; }
    public Guid? SensorId { get; set; }
    public Guid? LocationId { get; set; }
    public float Value { get; set; }
    public long Timestamp { get; set; }

    public Sensor? Sensor { get; set; }
    public Location? Location { get; set; }

    public SensorData(Guid? sensorId, Guid? locationId, float value, long timestamp)
    {
        Id = Guid.NewGuid();
        SensorId = sensorId;
        LocationId = locationId;
        Value = value;
        Timestamp = timestamp;
    }
}
