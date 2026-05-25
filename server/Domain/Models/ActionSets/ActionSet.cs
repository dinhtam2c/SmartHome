namespace Domain.Models.ActionSets;

public abstract class ActionSet
{
    private readonly List<ActionSetAction> _actions = [];

    public Guid Id { get; private set; }
    public ActionExecutionMode ExecutionMode { get; private set; }
    public bool ContinueOnError { get; private set; }

    public IReadOnlyCollection<ActionSetAction> Actions => _actions;
    public IReadOnlyList<ActionSetAction> BeforeActions => GetActions(ActionSetSection.Before);
    public IReadOnlyList<ActionSetAction> MainActions => GetActions(ActionSetSection.Main);
    public IReadOnlyList<ActionSetAction> OnSuccessActions => GetActions(ActionSetSection.OnSuccess);
    public IReadOnlyList<ActionSetAction> OnFailureActions => GetActions(ActionSetSection.OnFailure);

    protected virtual bool RequiresMainAction => false;

    protected ActionSet()
    {
    }

    protected ActionSet(ActionSetDefinition definition)
    {
        Id = Guid.NewGuid();
        Replace(definition, updateTimestamp: false);
    }

    public void Replace(ActionSetDefinition definition)
    {
        Replace(definition, updateTimestamp: true);
    }

    public ActionSetDefinition ToDefinition()
    {
        return new ActionSetDefinition(
            MainActions.Select(action => action.ToDefinition()).ToList(),
            new ActionSetHooksDefinition(
                BeforeActions.Select(action => action.ToDefinition()).ToList(),
                OnSuccessActions.Select(action => action.ToDefinition()).ToList(),
                OnFailureActions.Select(action => action.ToDefinition()).ToList()),
            new ActionSetExecutionPolicy(ExecutionMode, ContinueOnError));
    }

    private void Replace(ActionSetDefinition definition, bool updateTimestamp)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (RequiresMainAction && definition.Actions.Count == 0)
            throw new InvalidOperationException("Action set must contain at least one main action.");

        ExecutionMode = definition.ExecutionPolicy.Mode;
        ContinueOnError = definition.ExecutionPolicy.ContinueOnError;

        _actions.Clear();
        AddActions(definition.Actions, ActionSetSection.Main);
        AddActions(definition.Hooks.Before, ActionSetSection.Before);
        AddActions(definition.Hooks.OnSuccess, ActionSetSection.OnSuccess);
        AddActions(definition.Hooks.OnFailure, ActionSetSection.OnFailure);

        if (updateTimestamp)
            TouchOwner();
    }

    protected virtual void TouchOwner()
    {
    }

    private void AddActions(IReadOnlyList<ActionDefinition> actions, ActionSetSection section)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            _actions.Add(ActionSetAction.FromDefinition(Id, section, actions[index], index));
        }
    }

    private IReadOnlyList<ActionSetAction> GetActions(ActionSetSection section)
    {
        return _actions
            .Where(action => action.Section == section)
            .OrderBy(action => action.Order)
            .ToList();
    }
}
