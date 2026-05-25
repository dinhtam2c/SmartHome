namespace Application.Exceptions;

public class CommandNotSupportedException : BadRequestException
{
    public CommandNotSupportedException(string command, Guid deviceId)
        : base($"Command '{command}' is not supported by device {deviceId}") { }

    public CommandNotSupportedException()
        : base("Command not supported") { }
}
