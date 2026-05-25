using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Scenes;

public class SceneExecutionRepository : ISceneExecutionRepository
{
    private readonly AppDbContext _context;

    public SceneExecutionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(SceneExecution execution, CancellationToken ct = default)
    {
        _context.SceneExecutions.Add(execution);
        return Task.CompletedTask;
    }

    public async Task<SceneExecution?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.SceneExecutions
            .Include(execution => execution.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(execution => execution.Id == id, ct);
    }

    public async Task<SceneExecution?> GetByCommandCorrelation(
        Guid deviceId,
        string correlationId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;

        var normalizedCorrelationId = correlationId.ToLower();

        var executionId = await _context.SceneExecutionActions
            .Where(action =>
                action.DeviceId == deviceId
                && action.CommandCorrelationId != null
                && action.CommandCorrelationId.ToLower() == normalizedCorrelationId)
            .OrderByDescending(action => action.UpdatedAt)
            .Select(action => action.SceneExecutionId)
            .FirstOrDefaultAsync(ct);

        if (executionId == Guid.Empty)
            return null;

        return await _context.SceneExecutions
            .Include(execution => execution.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(execution => execution.Id == executionId, ct);
    }
}
