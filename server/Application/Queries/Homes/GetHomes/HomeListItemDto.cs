namespace Application.Queries.Homes.GetHomes;

public sealed record HomeListItemDto(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt
);
