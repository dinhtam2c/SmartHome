using Core.Entities;

namespace Application.DTOs.SensorDataDto;

public record SensorDataResponse(
    Guid Id,
    string Location,
    SensorType Type,
    string Unit,
    float Value,
    DateTime Timestamp
)
{
    public static SensorDataResponse FromSensorData(SensorData e)
    {
        return new(
            e.Id,
            e.Location,
            e.Sensor!.Type,
            e.Sensor!.Unit,
            e.Value,
            DateTimeOffset.FromUnixTimeSeconds(e.Timestamp).LocalDateTime
        );
    }
}
