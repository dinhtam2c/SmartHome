namespace Application.Exceptions;

public class InvalidStateException : BadRequestException
{
    public InvalidStateException(Guid deviceId, string state)
        : base($"Device {deviceId} sent unknown state: {state}") { }
}
