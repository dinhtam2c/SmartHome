using Domain.Models.ActionSets;

namespace Domain.Models.Scenes;

public sealed class SceneActionSet : ActionSet
{
    public Guid SceneId { get; private set; }

    private SceneActionSet()
    {
    }

    private SceneActionSet(Guid sceneId, ActionSetDefinition definition) : base(definition)
    {
        if (sceneId == Guid.Empty)
            throw new ArgumentException("SceneId is required.", nameof(sceneId));

        SceneId = sceneId;
    }

    public static SceneActionSet Create(Guid sceneId, ActionSetDefinition definition)
    {
        return new SceneActionSet(sceneId, definition);
    }
}
