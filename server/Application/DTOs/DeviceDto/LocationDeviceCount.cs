namespace Application.DTOs.DeviceDto;

public record LocationDeviceCount(
    Guid LocationId,
    int Total,
    int Online
);
