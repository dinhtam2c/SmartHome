namespace Domain.Models.Devices.Commands;

public enum CommandLifecycleStatus
{
    Pending,
    Completed,
    Failed,
    TimedOut
}
