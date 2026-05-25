namespace Core.Domain.Automations;

public interface IAutomationRuleRepository
{
    Task<AutomationRule?> GetById(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AutomationRule>> GetEnabledByConditionTarget(
        Guid homeId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        CancellationToken ct = default);
    Task Add(AutomationRule rule, CancellationToken ct = default);
    void Remove(AutomationRule rule);
}
