using Core.Entities;

namespace Application.DTOs.Messages.Gateways;

public record GatewayData(
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
    public SensorData ToSensorData(Guid? locationId, long timestamp)
    {
        return new(
            sensorId: SensorId,
            locationId: locationId,
            value: Value,
            timestamp: timestamp
        );
    }
}
