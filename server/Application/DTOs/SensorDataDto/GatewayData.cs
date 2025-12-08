using Core.Entities;

namespace Application.DTOs.SensorDataDto;

public record GatewayData(
    Guid GatewayId,
    long Timestamp,
    IEnumerable<DevicaData> Data
);

public record DevicaData(
    Guid DeviceId,
    long Timestamp,
    string Priority,
    IEnumerable<DeviceSensorData> Data
);

public record DeviceSensorData(
    Guid SensorId,
    float Value
)
{
    public SensorData ToSensorData(string location, SensorType type, string unit, long timestamp)
    {
        return new(
            id: Guid.NewGuid(),
            sensorId: SensorId,
            location: location,
            type: type,
            unit: unit,
            value: Value,
            timestamp: timestamp
        );
    }
}
