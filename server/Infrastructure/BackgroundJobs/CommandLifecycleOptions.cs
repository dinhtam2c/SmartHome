namespace Infrastructure.BackgroundJobs;

public sealed class CommandLifecycleOptions
{
    public int SweepIntervalSeconds { get; init; } = 5;
    public int PendingTimeoutSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 100;
}
