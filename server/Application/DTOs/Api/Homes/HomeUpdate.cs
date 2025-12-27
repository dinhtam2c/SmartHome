namespace Application.DTOs.Api.Homes;

public record HomeUpdateRequest(
    string? Name,
    string? Description
);
