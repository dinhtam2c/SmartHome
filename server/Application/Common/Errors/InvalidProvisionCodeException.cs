namespace Application.Common.Errors;

public class InvalidProvisionCodeException : BadRequestException
{
    public InvalidProvisionCodeException(string provisionCode)
        : base($"Invalid provision code: {provisionCode}") { }
}
