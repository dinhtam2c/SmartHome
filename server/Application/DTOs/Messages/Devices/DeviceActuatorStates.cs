using Core.Entities;

namespace Application.DTOs.Messages.Devices;

public record DeviceActuatorStates(
    Guid ActuatorId,
    Dictionary<ActuatorState, object> States
);
