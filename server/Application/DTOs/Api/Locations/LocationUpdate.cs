namespace Application.DTOs.Api.Locations;

public record LocationUpdateRequest(
    string? Name,
    string? Description
);
