using Core.Entities;

namespace Application.DTOs.LocationDto;

public record LocationListElement(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    long CreatedAt,
    long UpdatedAt
)
{
    public static LocationListElement FromLocation(Location location)
    {
        return new(
            Id: location.Id,
            HomeId: location.HomeId,
            Name: location.Name,
            Description: location.Description,
            CreatedAt: location.CreatedAt,
            UpdatedAt: location.UpdatedAt
        );
    }
}
