using Core.Entities;

namespace Application.DTOs.Api.Homes;

public record HomeListElement(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt,
    long UpdatedAt
)
{
    public static HomeListElement FromHome(Home home)
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
