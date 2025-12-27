using Core.Entities;

namespace Application.DTOs.Api.Dashboard;

public record HomeDashboardDto
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public HomeDashboardSummary Summary { get; }
    public IEnumerable<LocationElement> Locations { get; }

    public HomeDashboardDto(Home home, HomeDashboardSummary summary, IEnumerable<LocationElement> locations)
    {
        Id = home.Id;
        Name = home.Name;
        Description = home.Description;
        Summary = summary;
        Locations = locations;
    }
}

public record HomeDashboardSummary(
    int DeviceCount,
    int OnlineDeviceCount
);

public record LocationElement(
    Guid Id,
    string Name,
    string? Description,
    int DeviceCount,
    int OnlineDeviceCount
);
