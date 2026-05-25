using Domain.Models.ActionSets;
using Domain.Common;

namespace Domain.Models.Scenes;

public class Scene : Entity
{
    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public long CreatedAt { get; private set; }
    public long UpdatedAt { get; private set; }

    public SceneActionSet ActionSet { get; private set; } = null!;

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
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsEnabled = isEnabled;
        var now = UnixTime.Now();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Scene Create(
        Guid homeId,
        string name,
        string? description,
        bool isEnabled,
        ActionSetDefinition actionSet)
    {
        var scene = new Scene(homeId, name, description, isEnabled);
        scene.ReplaceActionSet(actionSet);
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

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (isEnabled.HasValue)
            IsEnabled = isEnabled.Value;

        UpdatedAt = UnixTime.Now();
    }

    public void ReplaceActionSet(ActionSetDefinition actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);

        if (ActionSet is null)
            ActionSet = SceneActionSet.Create(Id, actionSet);
        else
            ActionSet.Replace(actionSet);

        UpdatedAt = UnixTime.Now();
    }

}
