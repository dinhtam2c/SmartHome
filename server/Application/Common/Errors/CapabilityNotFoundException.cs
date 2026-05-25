namespace Application.Common.Errors;

public class CapabilityNotFoundException : NotFoundException
{
    public CapabilityNotFoundException(string capabilityId)
        : base($"Capability '{capabilityId}' not found")
    {
    }
}
