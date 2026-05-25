using Domain.Models.Automations;
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
            .Include(rule => rule.ActionSet)
            .ThenInclude(actionSet => actionSet.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(rule => rule.Id == id, ct);
    }

    public void Remove(AutomationRule rule)
    {
        _context.AutomationRules.Remove(rule);
    }
}
