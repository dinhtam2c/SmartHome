namespace Application.Exceptions;

public class InvalidProvisionCodeException : BadRequestException
{
    public InvalidProvisionCodeException(string provisionCode)
        : base($"Invalid provision code: {provisionCode}") { }
}
