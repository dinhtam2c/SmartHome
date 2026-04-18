namespace Application.Exceptions;

public class CapabilityNotFoundException : NotFoundException
{
    public CapabilityNotFoundException(string capabilityId)
        : base($"Capability '{capabilityId}' not found")
    {
    }
}
