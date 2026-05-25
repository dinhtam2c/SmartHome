using Core.Common;
using Core.Domain.ActionSets;
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

    public ActionExecutionMode ExecutionMode { get; private set; }
    public bool ContinueOnError { get; private set; }

    private readonly List<SceneAction> _actions = [];
    public IReadOnlyCollection<SceneAction> Actions => _actions;

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
        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;
        ExecutionMode = ActionSetExecutionPolicy.Default.Mode;
        ContinueOnError = ActionSetExecutionPolicy.Default.ContinueOnError;
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

        UpdatedAt = Time.UnixNow();
    }

    public void ReplaceActionSet(ActionSetDefinition actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);

        ExecutionMode = actionSet.ExecutionPolicy.Mode;
        ContinueOnError = actionSet.ExecutionPolicy.ContinueOnError;

        _actions.Clear();
        AddActions(actionSet.Actions, ActionSetSection.Main);
        AddActions(actionSet.Hooks.Before, ActionSetSection.Before);
        AddActions(actionSet.Hooks.OnSuccess, ActionSetSection.OnSuccess);
        AddActions(actionSet.Hooks.OnFailure, ActionSetSection.OnFailure);

        UpdatedAt = Time.UnixNow();
    }

    public ActionSetDefinition ToActionSetDefinition()
    {
        return new ActionSetDefinition(
            GetDefinitions(ActionSetSection.Main),
            new ActionSetHooksDefinition(
                GetDefinitions(ActionSetSection.Before),
                GetDefinitions(ActionSetSection.OnSuccess),
                GetDefinitions(ActionSetSection.OnFailure)),
            new ActionSetExecutionPolicy(ExecutionMode, ContinueOnError));
    }

    private void AddActions(IReadOnlyList<ActionDefinition> actions, ActionSetSection section)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            _actions.Add(SceneAction.FromDefinition(Id, section, actions[index], index));
        }
    }

    private IReadOnlyList<ActionDefinition> GetDefinitions(ActionSetSection section)
    {
        return _actions
            .Where(action => action.Section == section)
            .OrderBy(action => action.Order)
            .Select(action => action.ToDefinition())
            .ToList();
    }
}
