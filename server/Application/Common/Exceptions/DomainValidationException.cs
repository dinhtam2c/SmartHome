namespace Application.Exceptions;

public class DomainValidationException : BadRequestException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
