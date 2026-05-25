namespace Core.Domain.Automations;

public interface IAutomationExecutionRepository
{
    Task Add(AutomationExecution execution, CancellationToken ct = default);
    Task<AutomationExecution?> GetById(Guid id, CancellationToken ct = default);
    Task<AutomationExecution?> GetByCommandCorrelation(
        Guid deviceId,
        string correlationId,
        CancellationToken ct = default);
}
