using Application.BusinessServices.Devices.ControlLifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs;

public sealed class CommandLifecycleTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandLifecycleTimeoutWorker> _logger;
    private readonly CommandLifecycleOptions _options;

    public CommandLifecycleTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CommandLifecycleOptions> options,
        ILogger<CommandLifecycleTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.SweepIntervalSeconds));
        var timeoutSeconds = Math.Max(1, _options.PendingTimeoutSeconds);
        var batchSize = Math.Clamp(_options.BatchSize, 1, 1000);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<CommandTimeoutProcessor>();
                var cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timeoutSeconds;
                var count = await processor.Process(cutoff, batchSize, stoppingToken);

                if (count > 0)
                    _logger.LogInformation("Marked {Count} command execution(s) as timed out", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sweeping pending command executions for timeout");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
