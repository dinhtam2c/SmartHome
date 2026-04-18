using Core.Common;
using Core.Primitives;

namespace Core.Domain.Scenes;

public class Scene : Entity
{
    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public long CreatedAt { get; private set; }
    public long UpdatedAt { get; private set; }

    private readonly List<SceneTarget> _targets = [];
    public IReadOnlyCollection<SceneTarget> Targets => _targets;

    private readonly List<SceneSideEffect> _sideEffects = [];
    public IReadOnlyCollection<SceneSideEffect> SideEffects => _sideEffects;

    private Scene()
    {
        Name = string.Empty;
    }

    private Scene(Guid homeId, string name, string? description, bool isEnabled)
    {
        if (homeId == Guid.Empty)
            throw new ArgumentException("HomeId is required.", nameof(homeId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene name is required.", nameof(name));

        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = name.Trim();
        Description = description?.Trim();
        IsEnabled = isEnabled;
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Scene Create(
        Guid homeId,
        string name,
        string? description,
        bool isEnabled,
        IEnumerable<SceneTargetDefinition>? targets,
        IEnumerable<SceneSideEffectDefinition>? sideEffects = null)
    {
        var scene = new Scene(homeId, name, description, isEnabled);
        scene.ReplaceTargets(targets);
        scene.ReplaceSideEffects(sideEffects);
        return scene;
    }

    public void UpdateInfo(string? name, string? description, bool? isEnabled)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Scene name is required.", nameof(name));

            Name = name.Trim();
        }

        if (description is not null)
            Description = description.Trim();

        if (isEnabled.HasValue)
            IsEnabled = isEnabled.Value;

        UpdatedAt = Time.UnixNow();
    }

    public void ReplaceTargets(IEnumerable<SceneTargetDefinition>? targets)
    {
        var targetList = targets?.ToList() ?? [];
        _targets.Clear();
        for (var index = 0; index < targetList.Count; index++)
        {
            _targets.Add(SceneTarget.FromTargetDefinition(Id, targetList[index], index));
        }

        UpdatedAt = Time.UnixNow();
    }

    public void ReplaceSideEffects(IEnumerable<SceneSideEffectDefinition>? sideEffects)
    {
        var sideEffectList = sideEffects?.ToList() ?? [];

        _sideEffects.Clear();
        for (var index = 0; index < sideEffectList.Count; index++)
        {
            _sideEffects.Add(SceneSideEffect.FromDefinition(Id, sideEffectList[index], index));
        }

        UpdatedAt = Time.UnixNow();
    }

}
