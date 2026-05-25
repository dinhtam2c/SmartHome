using Core.Domain.Automations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Automations;

public class AutomationRuleRepository : IAutomationRuleRepository
{
    private readonly AppDbContext _context;

    public AutomationRuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(AutomationRule rule, CancellationToken ct = default)
    {
        _context.AutomationRules.Add(rule);
        return Task.CompletedTask;
    }

    public async Task<AutomationRule?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.AutomationRules
            .Include(rule => rule.Conditions)
            .Include(rule => rule.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(rule => rule.Id == id, ct);
    }

    public async Task<IReadOnlyList<AutomationRule>> GetEnabledByConditionTarget(
        Guid homeId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        CancellationToken ct = default)
    {
        var normalizedEndpointId = endpointId.Trim().ToLower();
        var normalizedCapabilityId = capabilityId.Trim().ToLower();

        return await _context.AutomationRules
            .Include(rule => rule.Conditions)
            .Include(rule => rule.Actions)
            .AsSplitQuery()
            .Where(rule => rule.HomeId == homeId && rule.IsEnabled)
            .Where(rule => rule.Conditions.Any(condition =>
                condition.DeviceId == deviceId
                && condition.EndpointId.ToLower() == normalizedEndpointId
                && condition.CapabilityId.ToLower() == normalizedCapabilityId))
            .ToListAsync(ct);
    }

    public void Remove(AutomationRule rule)
    {
        _context.AutomationRules.Remove(rule);
    }
}
