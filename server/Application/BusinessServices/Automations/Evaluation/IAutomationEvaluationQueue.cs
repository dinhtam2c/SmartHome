namespace Application.BusinessServices.Automations.Evaluation;

public interface IAutomationEvaluationQueue
{
    ValueTask EnqueueAsync(AutomationEvaluationWorkItem workItem, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AutomationEvaluationWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default);
}
