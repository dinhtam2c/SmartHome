namespace Core.Entities;

public class SensorData
{
    public Guid Id { get; set; }
    public Guid? SensorId { get; set; }
    public string Location { get; set; }
    public SensorType Type { get; set; }
    public string Unit { get; set; }
    public float Value { get; set; }
    public long Timestamp { get; set; }

    public Sensor? Sensor { get; set; }

    public SensorData(Guid id, Guid? sensorId, string location, SensorType type,
        string unit, float value, long timestamp)
    {
        Id = id;
        SensorId = sensorId;
        Location = location;
        Type = type;
        Unit = unit;
        Value = value;
        Timestamp = timestamp;
    }
}
