namespace Application.Exceptions;

public class InvalidStateException : BadRequestException
{
    public InvalidStateException(string state, string entityType)
        : base($"{entityType} sent unknown state: {state}") { }
}
