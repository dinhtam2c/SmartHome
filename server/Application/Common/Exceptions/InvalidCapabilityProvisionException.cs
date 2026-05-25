namespace Application.Exceptions;

public class InvalidCapabilityProvisionException : BadRequestException
{
    public InvalidCapabilityProvisionException()
        : base("Provision request must include at least one capability")
    {
    }

    public InvalidCapabilityProvisionException(string message)
        : base(message)
    {
    }
}
