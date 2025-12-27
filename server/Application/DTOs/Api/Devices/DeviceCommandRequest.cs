using Core.Entities;

namespace Application.DTOs.Api.Devices;

public record DeviceCommandRequest(
    Guid ActuatorId,
    ActuatorCommand Command,
    object? Parameters
);
