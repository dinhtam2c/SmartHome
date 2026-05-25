namespace Core.Domain.DeviceCommands;

public enum CommandLifecycleStatus
{
    Pending,
    Accepted,
    Completed,
    Failed,
    TimedOut
}
