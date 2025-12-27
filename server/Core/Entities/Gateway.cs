using Core.Common;

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

    public Gateway(string name, string? manufacturer, string? model,
        string firmwareVersion, string mac)
    {
        Id = Guid.NewGuid();
        Name = name;
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        Mac = mac;
        var now = Time.UnixNow();
        LastSeenAt = now;
        CreatedAt = now;
        UpdatedAt = now;

        Devices = [];
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
        DeviceCount = 0;
    }

    public void UpdateState(int uptime, int deviceCount)
    {
        Uptime = uptime;
        DeviceCount = deviceCount;
        LastSeenAt = Time.UnixNow();
    }

    public void UpdateFromProvision(string? manufacturer, string? model, string firmwareVersion)
    {
        Manufacturer = manufacturer;
        Model = model;
        FirmwareVersion = firmwareVersion;
        var now = Time.UnixNow();
        LastSeenAt = now;
        UpdatedAt = now;
    }

    public void AssignHome(Guid homeId)
    {
        HomeId = homeId;
        UpdatedAt = Time.UnixNow();
    }
}
