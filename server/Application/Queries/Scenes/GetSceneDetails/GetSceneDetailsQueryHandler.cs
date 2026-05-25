using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Scenes.GetSceneDetails;

public sealed class GetSceneDetailsQueryHandler : IRequestHandler<GetSceneDetailsQuery, SceneDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetSceneDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<SceneDetailsDto> Handle(
        GetSceneDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item => item.Id == request.SceneId && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        return new SceneDetailsDto(
            scene.Id,
            scene.HomeId,
            scene.Name,
            scene.Description,
            scene.IsEnabled,
            scene.CreatedAt,
            scene.UpdatedAt,
            ActionSetDtoMapper.ForScene(scene));
    }
}
