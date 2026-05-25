namespace Application.Common.Errors;

public sealed class DeviceCommandDispatchException : AppException
{
    public Guid CommandExecutionId { get; }

    public DeviceCommandDispatchException(Guid commandExecutionId, string message, Exception? dispatchException = null)
        : base(message, dispatchException)
    {
        CommandExecutionId = commandExecutionId;
    }
}
