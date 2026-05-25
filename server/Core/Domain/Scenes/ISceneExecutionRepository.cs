namespace Core.Domain.Scenes;

public interface ISceneExecutionRepository
{
    Task Add(SceneExecution execution, CancellationToken ct = default);
    Task<SceneExecution?> GetById(Guid id, CancellationToken ct = default);
    Task<SceneExecution?> GetByCommandCorrelation(Guid deviceId, string correlationId, CancellationToken ct = default);
}
