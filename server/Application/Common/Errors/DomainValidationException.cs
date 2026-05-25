namespace Application.Common.Errors;

public class DomainValidationException : BadRequestException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
