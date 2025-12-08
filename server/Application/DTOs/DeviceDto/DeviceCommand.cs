using Core.Entities;

namespace Application.DTOs.DeviceDto;

public record DeviceCommandRequest(
    Guid ActuatorId,
    ActuatorCommand Command,
    object? Parameters
);

// Payload send to gateway
public record DeviceCommand(
    Guid DeviceId,
    Guid ActuatorId,
    ActuatorCommand Command,
    object? Parameters
);
