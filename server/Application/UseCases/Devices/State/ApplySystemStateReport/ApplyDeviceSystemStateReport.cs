using MediatR;

namespace Application.UseCases.Devices.State.ApplySystemStateReport;

public sealed record ApplyDeviceSystemStateReport(
    Guid DeviceId,
    int Uptime
) : IRequest;
