using MediatR;

namespace Application.UseCases.Devices.Control.SendCommand;

public sealed class SendDeviceCommand : IRequest<Guid>
{
    public Guid DeviceId { get; }
    public string CapabilityId { get; }
    public string EndpointId { get; }
    public string Operation { get; }
    public object? Value { get; }
    public Guid CommandExecutionId { get; }
    public string CorrelationId { get; }

    private SendDeviceCommand(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        string operation,
        object? value)
    {
        DeviceId = deviceId;
        CapabilityId = capabilityId;
        EndpointId = endpointId;
        Operation = operation;
        Value = value;
        CommandExecutionId = Guid.NewGuid();
        CorrelationId = Guid.NewGuid().ToString("N");
    }

    public static SendDeviceCommand Create(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        string operation,
        object? value)
    {
        return new SendDeviceCommand(
            deviceId,
            capabilityId,
            endpointId,
            operation,
            value);
    }
}
