using Application.BusinessServices.ActionSets.Contracts;
using MediatR;

namespace Application.UseCases.Scenes.UpdateScene;

public sealed record UpdateSceneCommand(
    Guid HomeId,
    Guid SceneId,
    string? Name,
    string? Description,
    bool? IsEnabled,
    ActionSetInput? ActionSet
) : IRequest;
