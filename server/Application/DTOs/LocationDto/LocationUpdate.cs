namespace Application.DTOs.LocationDto;

public record LocationUpdateRequest(
    string? Name,
    string? Description
);
