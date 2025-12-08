namespace Application.DTOs.HomeDto;

public record HomeUpdateRequest(
    string? Name,
    string? Description
);
