using Application.BusinessServices.ActionSets.Contracts;
namespace Application.UseCases.Scenes.GetSceneDetails;

public sealed record SceneDetailsDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    long CreatedAt,
    long UpdatedAt,
    ActionSetView ActionSet
);
