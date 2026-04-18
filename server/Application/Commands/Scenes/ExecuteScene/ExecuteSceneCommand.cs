using MediatR;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed record ExecuteSceneCommand(
    Guid HomeId,
    Guid SceneId,
    string? TriggerSource,
    ExecuteSceneOptions? Options = null
) : IRequest<Guid>;

public sealed record ExecuteSceneOptions(
    IReadOnlyCollection<string>? OnlyEndpoints = null,
    IReadOnlyCollection<string>? ExcludeCapabilities = null
);
