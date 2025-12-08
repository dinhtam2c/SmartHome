namespace Core.Entities;

public class Device
{
    public Guid Id { get; set; }
    public Guid? GatewayId { get; set; }
    public Guid? LocationId { get; set; }
    public string Identifier { get; set; }
    public string Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public bool IsOnline { get; set; }
    public long LastSeenAt { get; set; }
    public long UpTime { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public Gateway? Gateway { get; set; }
    public Location? Location { get; set; }
    public ICollection<Sensor> Sensors { get; set; }
    public ICollection<Actuator> Actuators { get; set; }

    public Device(Guid id, Guid? gatewayId, string identifier, string name, long createdAt)
    {
        Id = id;
        GatewayId = gatewayId;
        Identifier = identifier;
        Name = name;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        Sensors = [];
        Actuators = [];
    }
}
