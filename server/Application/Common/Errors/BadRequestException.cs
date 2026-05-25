namespace Application.Common.Errors;

public abstract class BadRequestException : AppException
{
    protected BadRequestException(string message) : base(message) { }
}
