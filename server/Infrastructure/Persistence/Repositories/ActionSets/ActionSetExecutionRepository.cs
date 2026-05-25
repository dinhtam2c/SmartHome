using Domain.Models.ActionSets;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.ActionSets;

public sealed class ActionSetExecutionRepository : IActionSetExecutionRepository
{
    private readonly AppDbContext _context;

    public ActionSetExecutionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(ActionSetExecution execution, CancellationToken cancellationToken = default)
    {
        _context.ActionSetExecutions.Add(execution);
        return Task.CompletedTask;
    }

    public async Task<ActionSetExecution?> GetByDeviceCommandExecutionId(
        Guid deviceCommandExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (deviceCommandExecutionId == Guid.Empty)
            return null;

        var executionId = await _context.ActionSetActionExecutions
            .Where(action => action.DeviceCommandExecutionId == deviceCommandExecutionId)
            .Select(action => action.ExecutionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (executionId == Guid.Empty)
            return null;

        return await _context.ActionSetExecutions
            .Include(execution => execution.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(execution => execution.Id == executionId, cancellationToken);
    }
}
