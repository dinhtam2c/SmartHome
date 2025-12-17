using Core.Entities;

namespace Application.DTOs.LocationDto;

public record LocationAddRequest(
    Guid HomeId,
    string Name,
    string? Description
)
{
    public Location ToLocation()
    {
        return new(
            id: Guid.NewGuid(),
            homeId: HomeId,
            name: Name,
            description: Description,
            createdAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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
