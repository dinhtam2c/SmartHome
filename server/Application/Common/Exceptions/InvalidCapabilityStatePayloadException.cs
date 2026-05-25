namespace Application.Exceptions;

public class InvalidCapabilityStatePayloadException : BadRequestException
{
    public InvalidCapabilityStatePayloadException(string capabilityId, string message)
        : base($"Invalid state payload for capability '{capabilityId}': {message}")
    {
    }
}
