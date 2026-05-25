using Domain.Models.ActionSets;
using Domain.Common;

namespace Domain.Models.Automations;

public class AutomationRule : Entity
{
    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public AutomationConditionLogic ConditionLogic { get; private set; }
    public int CooldownMs { get; private set; }
    public bool? LastEvaluationResult { get; private set; }
    public long? LastEvaluatedAt { get; private set; }
    public long? LastTriggeredAt { get; private set; }
    public bool TimeWindowEnabled { get; private set; }
    public int? TimeWindowStartMinute { get; private set; }
    public int? TimeWindowEndMinute { get; private set; }
    public int TimeWindowDaysOfWeekMask { get; private set; }
    public long CreatedAt { get; private set; }
    public long UpdatedAt { get; private set; }

    private readonly List<AutomationCondition> _conditions = [];
    public IReadOnlyCollection<AutomationCondition> Conditions => _conditions;

    public AutomationActionSet ActionSet { get; private set; } = null!;

    private AutomationRule()
    {
        Name = string.Empty;
    }

    private AutomationRule(
        Guid homeId,
        string name,
        string? description,
        bool isEnabled,
        AutomationConditionLogic conditionLogic,
        int cooldownMs)
    {
        if (homeId == Guid.Empty)
            throw new ArgumentException("HomeId is required.", nameof(homeId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Automation name is required.", nameof(name));

        if (cooldownMs < 0)
            throw new ArgumentException("CooldownMs must be >= 0.", nameof(cooldownMs));

        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsEnabled = isEnabled;
        ConditionLogic = conditionLogic;
        CooldownMs = cooldownMs;
        var now = UnixTime.Now();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static AutomationRule Create(
        Guid homeId,
        string name,
        string? description,
        bool isEnabled,
        AutomationConditionLogic conditionLogic,
        int cooldownMs,
        IEnumerable<AutomationConditionDefinition> conditions,
        AutomationTimeWindowDefinition? timeWindow,
        ActionSetDefinition actionSet)
    {
        var rule = new AutomationRule(homeId, name, description, isEnabled, conditionLogic, cooldownMs);
        rule.ReplaceConditions(conditions);
        rule.ReplaceTimeWindow(timeWindow);
        rule.ReplaceActionSet(actionSet);
        return rule;
    }

    public void UpdateInfo(
        string? name,
        string? description,
        bool? isEnabled,
        AutomationConditionLogic? conditionLogic,
        int? cooldownMs)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Automation name is required.", nameof(name));

            Name = name.Trim();
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (isEnabled.HasValue)
            IsEnabled = isEnabled.Value;

        if (conditionLogic.HasValue)
            ConditionLogic = conditionLogic.Value;

        if (cooldownMs.HasValue)
        {
            if (cooldownMs.Value < 0)
                throw new ArgumentException("CooldownMs must be >= 0.", nameof(cooldownMs));

            CooldownMs = cooldownMs.Value;
        }

        UpdatedAt = UnixTime.Now();
    }

    public void ReplaceConditions(IEnumerable<AutomationConditionDefinition> conditions)
    {
        var conditionList = conditions?.ToList() ?? [];
        if (conditionList.Count == 0)
            throw new InvalidOperationException("Automation rule must contain at least one device-state condition.");

        _conditions.Clear();
        for (var index = 0; index < conditionList.Count; index++)
        {
            _conditions.Add(AutomationCondition.FromDefinition(Id, conditionList[index], index));
        }

        UpdatedAt = UnixTime.Now();
    }

    public void ReplaceTimeWindow(AutomationTimeWindowDefinition? timeWindow)
    {
        if (timeWindow is null)
        {
            TimeWindowEnabled = false;
            TimeWindowStartMinute = null;
            TimeWindowEndMinute = null;
            TimeWindowDaysOfWeekMask = 0;
            UpdatedAt = UnixTime.Now();
            return;
        }

        TimeWindowEnabled = true;
        TimeWindowStartMinute = timeWindow.StartMinute;
        TimeWindowEndMinute = timeWindow.EndMinute;
        TimeWindowDaysOfWeekMask = timeWindow.DaysOfWeekMask;
        UpdatedAt = UnixTime.Now();
    }

    public void ReplaceActionSet(ActionSetDefinition actionSet)
    {
        ArgumentNullException.ThrowIfNull(actionSet);
        if (ActionSet is null)
            ActionSet = AutomationActionSet.Create(Id, actionSet);
        else
            ActionSet.Replace(actionSet);

        UpdatedAt = UnixTime.Now();
    }

    public void MarkEvaluated(bool result, long? evaluatedAt = null)
    {
        LastEvaluationResult = result;
        LastEvaluatedAt = evaluatedAt ?? UnixTime.Now();
        UpdatedAt = LastEvaluatedAt.Value;
    }

    public void MarkTriggered(long? triggeredAt = null)
    {
        var now = triggeredAt ?? UnixTime.Now();
        LastEvaluationResult = true;
        LastEvaluatedAt = now;
        LastTriggeredAt = now;
        UpdatedAt = now;
    }

    public bool IsCooldownSatisfied(long now)
    {
        if (!LastTriggeredAt.HasValue)
            return true;

        var cooldownSeconds = (int)Math.Ceiling(CooldownMs / 1000d);
        return now - LastTriggeredAt.Value >= cooldownSeconds;
    }
}
