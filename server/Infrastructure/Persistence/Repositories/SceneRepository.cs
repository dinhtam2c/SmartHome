using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SceneRepository : ISceneRepository
{
    private readonly AppDbContext _context;

    public SceneRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Scene scene, CancellationToken ct = default)
    {
        _context.Scenes.Add(scene);
        return Task.CompletedTask;
    }

    public async Task<Scene?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.Scenes
            .Include(scene => scene.Targets)
            .Include(scene => scene.SideEffects)
            .AsSplitQuery()
            .FirstOrDefaultAsync(scene => scene.Id == id, ct);
    }

    public void Remove(Scene scene)
    {
        _context.Scenes.Remove(scene);
    }
}
