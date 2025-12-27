using Core.Entities;

namespace Application.DTOs.Api.Dashboard;

public record LocationDashboardDto
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public LocationDashboardSummary Summary { get; }
    public IEnumerable<DeviceElement> Devices { get; }

    public LocationDashboardDto(Location location, LocationDashboardSummary summary, IEnumerable<DeviceElement> devices)
    {
        Id = location.Id;
        Name = location.Name;
        Description = location.Description;
        Summary = summary;
        Devices = devices;
    }
}

public record LocationDashboardSummary(
    int DeviceCount,
    int OnlineDeviceCount
);

public record DeviceElement(
    Guid Id,
    string Name,
    bool IsOnline,
    IEnumerable<SensorDataDto> LatestSensorData,
    IEnumerable<ActuatorStateDto> ActuatorStates
);
