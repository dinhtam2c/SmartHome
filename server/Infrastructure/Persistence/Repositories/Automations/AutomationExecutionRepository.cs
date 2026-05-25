using Core.Domain.Automations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Automations;

public class AutomationExecutionRepository : IAutomationExecutionRepository
{
    private readonly AppDbContext _context;

    public AutomationExecutionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(AutomationExecution execution, CancellationToken ct = default)
    {
        _context.AutomationExecutions.Add(execution);
        return Task.CompletedTask;
    }

    public async Task<AutomationExecution?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.AutomationExecutions
            .Include(execution => execution.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(execution => execution.Id == id, ct);
    }

    public async Task<AutomationExecution?> GetByCommandCorrelation(
        Guid deviceId,
        string correlationId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;

        var normalizedCorrelationId = correlationId.ToLower();
        var executionId = await _context.AutomationExecutionActions
            .Where(action =>
                action.DeviceId == deviceId
                && action.CommandCorrelationId != null
                && action.CommandCorrelationId.ToLower() == normalizedCorrelationId)
            .OrderByDescending(action => action.UpdatedAt)
            .Select(action => action.AutomationExecutionId)
            .FirstOrDefaultAsync(ct);

        if (executionId == Guid.Empty)
            return null;

        return await _context.AutomationExecutions
            .Include(execution => execution.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(execution => execution.Id == executionId, ct);
    }
}
