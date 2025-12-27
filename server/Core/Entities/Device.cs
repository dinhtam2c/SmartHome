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
    public long Uptime { get; set; }
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

    public void MarkOnline()
    {
        IsOnline = true;
        LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void MarkOffline()
    {
        IsOnline = false;
        Uptime = 0;
    }

    public void UpdateSystemState(int uptime)
    {
        Uptime = uptime;
        LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void UpdateFromProvision(string? name, string? manufacturer, string? model, string? firmwareVersion,
        ICollection<Sensor> sensors, ICollection<Actuator> actuators)
    {
        if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(name))
            Name = name;
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Sensors = sensors;
        Actuators = actuators;
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void AssignLocation(Guid? locationId)
    {
        LocationId = locationId;
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void AssignGateway(Guid? gatewayId)
    {
        GatewayId = gatewayId;
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
