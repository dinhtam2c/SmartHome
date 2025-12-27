using Core.Entities;

namespace Application.DTOs.Api.Homes;

public record HomeAddRequest(
    string Name,
    string? Description
)
{
    public Home ToHome()
    {
        return new(
            id: Guid.NewGuid(),
            name: Name,
            description: Description,
            createdAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
    }
}

public record HomeAddResponse(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt
)
{
    public static HomeAddResponse FromHome(Home home)
    {
        return new(
            Id: home.Id,
            Name: home.Name,
            Description: home.Description,
            CreatedAt: home.CreatedAt
        );
    }
}
