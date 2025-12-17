using Core.Entities;

namespace Application.DTOs.DeviceDto;

public record DeviceActuatorStates(
    Guid ActuatorId,
    Dictionary<ActuatorState, object> States
);
