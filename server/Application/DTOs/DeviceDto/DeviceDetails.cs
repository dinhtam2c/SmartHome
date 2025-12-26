using Core.Entities;

namespace Application.DTOs.DeviceDto;

public record DeviceDetails(
    Guid Id,
    string Identifier,
    string Name,
    Guid? GatewayId,
    string? GatewayName,
    Guid? LocationId,
    string? LocationName,
    string? Manufacturer,
    string? Model,
    string? FirmwareVersion,
    long CreatedAt,
    long UpdatedAt,
    IEnumerable<SensorDetail>? Sensors,
    IEnumerable<ActuatorDetail>? Actuators
)
{
    public static DeviceDetails FromDevice(Device device)
    {
        return new(
            Id: device.Id,
            Identifier: device.Identifier,
            Name: device.Name,
            GatewayId: device.GatewayId,
            GatewayName: device.Gateway?.Name,
            LocationId: device.LocationId,
            LocationName: device.Location?.Name,
            Manufacturer: device.Manufacturer,
            Model: device.Model,
            FirmwareVersion: device.FirmwareVersion,
            CreatedAt: device.CreatedAt,
            UpdatedAt: device.UpdatedAt,
            Sensors: device.Sensors?.Select(SensorDetail.FromSensor),
            Actuators: device.Actuators.Select(a => ActuatorDetail.FromActuator(a))
        );
    }
}

public record SensorDetail(
    Guid Id,
    string Name,
    SensorType Type,
    string Unit,
    float Min,
    float Max,
    float Accuracy
)
{
    public static SensorDetail FromSensor(Sensor sensor)
    {
        return new(
            Id: sensor.Id,
            Name: sensor.Name,
            Type: sensor.Type,
            Unit: sensor.Unit,
            Min: sensor.Min,
            Max: sensor.Max,
            Accuracy: sensor.Accuracy
        );
    }
}

public record ActuatorDetail(
    Guid Id,
    string Name,
    ActuatorType Type,
    IEnumerable<ActuatorState>? SupportedStates,
    IEnumerable<ActuatorCommand>? SupportedCommands
)
{
    public static ActuatorDetail FromActuator(Actuator actuator)
    {
        return new(
            Id: actuator.Id,
            Name: actuator.Name,
            Type: actuator.Type,
            SupportedStates: actuator.SupportedStates,
            SupportedCommands: actuator.SupportedCommands
        );
    }
}
