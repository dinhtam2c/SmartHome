using Core.Entities;

namespace Application.DTOs.Api.Locations;

public record LocationAddRequest(
    Guid HomeId,
    string Name,
    string? Description
)
{
    public Location ToLocation()
    {
        return new(
            homeId: HomeId,
            name: Name,
            description: Description
        );
    }
}

public record LocationAddResponse(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    long CreatedAt
)
{
    public static LocationAddResponse FromLocation(Location location)
    {
        return new(
            Id: location.Id,
            HomeId: location.HomeId,
            Name: location.Name,
            Description: location.Description,
            CreatedAt: location.CreatedAt
        );
    }
}
