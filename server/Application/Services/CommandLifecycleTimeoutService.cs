using Application.Common.Data;
using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Core.Domain.Scenes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class CommandLifecycleOptions
{
    public int PendingTimeoutSeconds { get; set; } = 30;

    public int SweepIntervalSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 200;
}

public class CommandLifecycleTimeoutService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandLifecycleTimeoutService> _logger;
    private readonly CommandLifecycleOptions _options;

    public CommandLifecycleTimeoutService(IServiceScopeFactory scopeFactory,
        IOptions<CommandLifecycleOptions> options,
        ILogger<CommandLifecycleTimeoutService> logger)
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
                var repository = scope.ServiceProvider.GetRequiredService<IDeviceCommandExecutionRepository>();
                var sceneExecutionRepository = scope.ServiceProvider.GetRequiredService<ISceneExecutionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timeoutSeconds;
                var staleRecords = await repository.GetPendingOlderThan(cutoff, batchSize);

                var timedOutCount = 0;
                foreach (var record in staleRecords)
                {
                    record.MarkTimedOut(record.Error);

                    var sceneExecution = await sceneExecutionRepository.GetByTargetCorrelation(
                        record.DeviceId,
                        record.CorrelationId,
                        stoppingToken);

                    if (sceneExecution is not null)
                    {
                        sceneExecution.TryApplyCommandLifecycle(
                            record.DeviceId,
                            record.CorrelationId,
                            CommandLifecycleStatus.TimedOut,
                            record.Error);
                    }

                    timedOutCount++;
                }

                if (timedOutCount > 0)
                {
                    await unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Marked {Count} command execution(s) as timed out", timedOutCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sweeping pending command executions for timeout");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
