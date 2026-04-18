namespace Application.Exceptions;

public class InvalidCapabilityCommandPayloadException : BadRequestException
{
    public InvalidCapabilityCommandPayloadException(string capabilityId, string message)
        : base($"Invalid payload for capability '{capabilityId}': {message}")
    {
    }
}
