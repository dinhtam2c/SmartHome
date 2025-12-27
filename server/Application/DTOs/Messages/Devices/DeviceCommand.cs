using Core.Entities;

namespace Application.DTOs.Messages.Devices;

public record DeviceCommand(
    Guid DeviceId,
    Guid ActuatorId,
    ActuatorCommand Command,
    object? Parameters
);
