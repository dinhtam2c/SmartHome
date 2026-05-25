using Application.BusinessServices.ActionSets.Contracts;
using MediatR;

namespace Application.UseCases.Scenes.CreateScene;

public sealed record CreateSceneCommand(
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    ActionSetInput? ActionSet
) : IRequest<Guid>;
