namespace Core.Entities;

public class Gateway
{
    public Guid Id { get; set; }
    public Guid? HomeId { get; set; }
    public string Name { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string FirmwareVersion { get; set; }
    public string Mac { get; set; }
    public bool IsOnline { get; set; }
    public long LastSeenAt { get; set; }
    public long Uptime { get; set; }
    public int DeviceCount { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public Home? Home { get; set; }
    public ICollection<Device> Devices { get; set; }

    public Gateway(Guid id, string name, string? manufacturer, string? model,
        string firmwareVersion, string mac, long createdAt)
    {
        Id = id;
        Name = name;
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        Mac = mac;
        LastSeenAt = createdAt;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;

        Devices = [];
    }
}
