using Core.Entities;

namespace Application.DTOs.SensorDataDto;

public record SensorDataResponse(
    Guid Id,
    Guid? LocationId,
    SensorType Type,
    string Unit,
    float Value,
    long Timestamp
)
{
    public static SensorDataResponse FromSensorData(SensorData e)
    {
        return new(
            e.Id,
            e.LocationId,
            e.Sensor!.Type,
            e.Sensor!.Unit,
            e.Value,
            e.Timestamp
        );
    }
}
