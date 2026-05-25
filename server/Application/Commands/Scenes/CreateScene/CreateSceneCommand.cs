using MediatR;

namespace Application.Commands.Scenes.CreateScene;

public sealed record CreateSceneCommand(
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    ActionSetModel? ActionSet
) : IRequest<Guid>;
