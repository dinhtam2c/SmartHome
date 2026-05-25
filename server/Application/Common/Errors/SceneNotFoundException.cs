namespace Application.Common.Errors;

public class SceneNotFoundException : NotFoundException
{
    public SceneNotFoundException(Guid sceneId)
        : base($"Scene '{sceneId}' not found")
    {
    }
}
