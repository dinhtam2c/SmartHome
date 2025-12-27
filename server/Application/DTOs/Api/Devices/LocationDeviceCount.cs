namespace Application.DTOs.Api.Devices;

public record LocationDeviceCount(
    Guid LocationId,
    int Total,
    int Online
);
