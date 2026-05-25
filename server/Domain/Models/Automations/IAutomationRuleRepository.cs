namespace Domain.Models.Automations;

public interface IAutomationRuleRepository
{
    Task<AutomationRule?> GetById(Guid id, CancellationToken ct = default);
    Task Add(AutomationRule rule, CancellationToken ct = default);
    void Remove(AutomationRule rule);
}
