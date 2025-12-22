using Core.Entities;

namespace Application.DTOs.DashboardDto;

public record DeviceDashboardDto
{
    public Guid Id { get; }
    public string Name { get; }
    public bool IsOnline { get; }
    public long UpTime { get; }
    public long LastSeenAt { get; }
    public IEnumerable<SensorDataDto> LatestSensorData { get; }
    public IEnumerable<ActuatorStateDto> ActuatorStates { get; }

    public DeviceDashboardDto(Device device, IEnumerable<SensorDataDto> latestSensorData)
    {
        Id = device.Id;
        Name = device.Name;
        IsOnline = device.IsOnline;
        UpTime = device.UpTime;
        LastSeenAt = device.LastSeenAt;
        LatestSensorData = latestSensorData;
        ActuatorStates = device.Actuators.Select(a =>
            new ActuatorStateDto(a.Id, a.Name, device.IsOnline ? a.States : null));
    }
}

public record SensorDataDto(
    Guid SensorId,
    string SensorName,
    SensorType Type,
    string Unit,
    float Value,
    long Timestamp
);

public record ActuatorStateDto(
    Guid ActuatorId,
    string ActuatorName,
    Dictionary<ActuatorState, object?>? States
);

