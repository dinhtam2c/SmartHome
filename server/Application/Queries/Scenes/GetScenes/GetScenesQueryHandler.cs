using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Scenes.GetScenes;

public sealed class GetScenesQueryHandler : IRequestHandler<GetScenesQuery, IReadOnlyList<SceneListItemDto>>
{
    private readonly IAppReadDbContext _context;

    public GetScenesQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SceneListItemDto>> Handle(
        GetScenesQuery request,
        CancellationToken cancellationToken)
    {
        var homeExists = await _context.Homes
            .AsNoTracking()
            .AnyAsync(home => home.Id == request.HomeId, cancellationToken);

        if (!homeExists)
            throw new HomeNotFoundException(request.HomeId);

        return await _context.Scenes
            .AsNoTracking()
            .Where(scene => scene.HomeId == request.HomeId)
            .OrderBy(scene => scene.Name)
            .Select(scene => new SceneListItemDto(
                scene.Id,
                scene.HomeId,
                scene.Name,
                scene.Description,
                scene.IsEnabled,
                scene.Targets.Count,
                scene.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
