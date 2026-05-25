namespace Application.Devices.CommandLifecycle;

public class CommandLifecycleOptions
{
    public int PendingTimeoutSeconds { get; set; } = 30;

    public int SweepIntervalSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 200;
}
