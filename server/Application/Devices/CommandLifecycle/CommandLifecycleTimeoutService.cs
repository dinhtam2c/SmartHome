using Application.Common.Data;
using Application.Common.Realtime;
using Core.Domain.Automations;
using Core.Domain.DeviceCommands;
using Core.Domain.Scenes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Devices.CommandLifecycle;

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

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IDeviceCommandExecutionRepository>();
                    var sceneExecutionRepository = scope.ServiceProvider.GetRequiredService<ISceneExecutionRepository>();
                    var automationExecutionRepository = scope.ServiceProvider.GetRequiredService<IAutomationExecutionRepository>();
                    var actionSetProcessor = scope.ServiceProvider.GetRequiredService<IActionSetProcessor>();
                    var realtimePublisher = scope.ServiceProvider.GetRequiredService<IRealtimePublisher>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timeoutSeconds;
                    var staleRecords = await repository.GetPendingOlderThan(cutoff, batchSize);

                    var timedOutCount = 0;
                    var changedCommandExecutions = new List<DeviceCommandExecution>();
                    var changedSceneExecutions = new List<SceneExecution>();
                    var changedAutomationExecutions = new List<AutomationExecution>();
                    foreach (var record in staleRecords)
                    {
                        record.MarkTimedOut(record.Error);
                        changedCommandExecutions.Add(record);

                        var sceneExecution = await sceneExecutionRepository.GetByCommandCorrelation(
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
                            await actionSetProcessor.AdvanceScene(sceneExecution, stoppingToken);
                            changedSceneExecutions.Add(sceneExecution);
                        }

                        var automationExecution = await automationExecutionRepository.GetByCommandCorrelation(
                            record.DeviceId,
                            record.CorrelationId,
                            stoppingToken);

                        if (automationExecution is not null)
                        {
                            automationExecution.TryApplyCommandLifecycle(
                                record.DeviceId,
                                record.CorrelationId,
                                CommandLifecycleStatus.TimedOut,
                                record.Error);
                            await actionSetProcessor.AdvanceAutomation(automationExecution, stoppingToken);
                            changedAutomationExecutions.Add(automationExecution);
                        }

                        timedOutCount++;
                    }

                    if (timedOutCount > 0)
                    {
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        await PublishRealtimeUpdates(
                            realtimePublisher,
                            changedCommandExecutions,
                            changedSceneExecutions,
                            changedAutomationExecutions,
                            stoppingToken);
                        _logger.LogInformation("Marked {Count} command execution(s) as timed out", timedOutCount);
                    }
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private static async Task PublishRealtimeUpdates(
        IRealtimePublisher realtimePublisher,
        IEnumerable<DeviceCommandExecution> commandExecutions,
        IEnumerable<SceneExecution> sceneExecutions,
        IEnumerable<AutomationExecution> automationExecutions,
        CancellationToken cancellationToken)
    {
        foreach (var execution in commandExecutions.DistinctBy(execution => execution.Id))
        {
            await realtimePublisher.PublishToDevice(
                execution.DeviceId,
                RealtimeDeltaFactory.ForDeviceCommandExecution(execution),
                cancellationToken);
        }

        foreach (var execution in sceneExecutions.DistinctBy(execution => execution.Id))
        {
            await realtimePublisher.PublishToHome(
                execution.HomeId,
                RealtimeDeltaFactory.ForSceneExecution(execution),
                cancellationToken);
        }

        foreach (var execution in automationExecutions.DistinctBy(execution => execution.Id))
        {
            await realtimePublisher.PublishToHome(
                execution.HomeId,
                RealtimeDeltaFactory.ForAutomationExecution(execution),
                cancellationToken);
        }
    }
}
