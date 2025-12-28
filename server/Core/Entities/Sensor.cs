namespace Core.Entities;

public class Sensor
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string Name { get; set; }
    public SensorType Type { get; set; }
    public string Unit { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public float Accuracy { get; set; }

    public Device? Device { get; set; }

    public Sensor(Guid deviceId, string name, SensorType type, string unit,
        float min, float max, float accuracy)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        Name = name;
        Type = type;
        Unit = unit;
        Min = min;
        Max = max;
        Accuracy = accuracy;
    }
}

public enum SensorType
{
    Temperature,
    Humidity,
    Light,
    Motion
}
