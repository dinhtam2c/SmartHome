using Core.Common;

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

    public Device(Guid? gatewayId, string identifier, string name)
    {
        Id = Guid.NewGuid();
        GatewayId = gatewayId;
        Identifier = identifier;
        Name = name;
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;

        Sensors = [];
        Actuators = [];
    }

    public void MarkOnline()
    {
        IsOnline = true;
        LastSeenAt = Time.UnixNow();
    }

    public void MarkOffline()
    {
        IsOnline = false;
        Uptime = 0;
    }

    public void UpdateSystemState(int uptime)
    {
        Uptime = uptime;
        LastSeenAt = Time.UnixNow();
    }

    public void UpdateFromProvision(string? name, string? manufacturer, string? model, string? firmwareVersion,
        ICollection<Sensor> sensors, ICollection<Actuator> actuators)
    {
        if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(name))
            Name = name;
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        LastSeenAt = Time.UnixNow();

        Sensors.Clear();
        foreach (var sensor in sensors)
        {
            Sensors.Add(sensor);
        }
        Actuators.Clear();
        foreach (var actuator in actuators)
        {
            Actuators.Add(actuator);
        }

        UpdatedAt = Time.UnixNow();
    }

    public void AssignLocation(Guid? locationId)
    {
        LocationId = locationId;
        UpdatedAt = Time.UnixNow();
    }

    public void AssignGateway(Guid? gatewayId)
    {
        GatewayId = gatewayId;
        UpdatedAt = Time.UnixNow();
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = Time.UnixNow();
    }
}
