namespace Core.Domain.Scenes;

public interface ISceneRepository
{
    Task<Scene?> GetById(Guid id, CancellationToken ct = default);
    Task Add(Scene scene, CancellationToken ct = default);
    void Remove(Scene scene);
}
