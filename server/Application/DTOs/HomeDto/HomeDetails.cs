using Core.Entities;

namespace Application.DTOs.HomeDto;

public record HomeDetails(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt,
    long UpdatedAt
)
{
    public static HomeDetails FromHome(Home home)
    {
        return new(
            Id: home.Id,
            Name: home.Name,
            Description: home.Description,
            CreatedAt: home.CreatedAt,
            UpdatedAt: home.UpdatedAt
        );
    }
}
