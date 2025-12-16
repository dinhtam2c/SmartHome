using Core.Entities;

namespace Application.DTOs.ProvisionDto;

public record DeviceProvisionResponse(
    string? DeviceIdentifier,
    Guid? DeviceId,
    IEnumerable<Guid>? SensorIds,
    IEnumerable<Guid>? ActuatorIds
);

public record DeviceProvisionRequest(
    string Key,
    string Identifier,
    string Name,
    string? Manufacturer,
    string? Model,
    string FirmwareVersion,
    long Timestamp,
    IEnumerable<DeviceSensor>? Sensors,
    IEnumerable<DeviceActuator>? Actuators
);

public record DeviceSensor(
    string Name,
    SensorType Type,
    string Unit,
    float Min,
    float Max,
    float Accuracy
)
{
    public Sensor ToSensor(Guid deviceId)
    {
        return new(
            id: Guid.NewGuid(),
            deviceId: deviceId,
            name: Name,
            type: Type,
            unit: Unit,
            min: Min,
            max: Max,
            accuracy: Accuracy);
    }
}

public record DeviceActuator(
    string Name,
    ActuatorType Type,
    IEnumerable<ActuatorState>? States,
    IEnumerable<ActuatorCommand>? Commands
)
{
    public Actuator ToActuator(Guid deviceId)
    {
        return new(
            id: Guid.NewGuid(),
            deviceId: deviceId,
            name: Name,
            type: Type,
            supportedStates: States,
            supportedCommands: Commands);
    }
}
