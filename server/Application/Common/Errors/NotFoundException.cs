namespace Application.Common.Errors;

public abstract class NotFoundException : AppException
{
    protected NotFoundException(string message) : base(message) { }
}
