using Application.BusinessServices.Automations.Evaluation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public sealed class AutomationEvaluationWorker : BackgroundService
{
    private readonly IAutomationEvaluationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationEvaluationWorker> _logger;

    public AutomationEvaluationWorker(
        IAutomationEvaluationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationEvaluationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var workItem in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<AutomationEvaluationProcessor>();
                    await processor.Process(workItem, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while evaluating automations for {CapabilityId}@{EndpointId} on device {DeviceId}",
                        workItem.CapabilityId,
                        workItem.EndpointId,
                        workItem.DeviceId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
