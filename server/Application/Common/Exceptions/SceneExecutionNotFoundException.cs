namespace Application.Exceptions;

public class SceneExecutionNotFoundException : NotFoundException
{
    public SceneExecutionNotFoundException(Guid executionId)
        : base($"Scene execution '{executionId}' not found")
    {
    }
}
