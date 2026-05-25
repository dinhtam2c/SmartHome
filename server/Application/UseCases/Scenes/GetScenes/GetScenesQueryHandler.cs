using Application.Ports.Persistence;
using Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Scenes.GetScenes;

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
                scene.ActionSet.Actions.Count(action => action.Section == Domain.Models.ActionSets.ActionSetSection.Main),
                scene.ActionSet.Actions.Count(action => action.Section != Domain.Models.ActionSets.ActionSetSection.Main),
                scene.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
