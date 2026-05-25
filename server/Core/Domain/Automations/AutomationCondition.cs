using System.Text.Json;
using Core.Common;

namespace Core.Domain.Automations;

public class AutomationCondition
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string FieldPath { get; private set; }
    public AutomationConditionOperator Operator { get; private set; }
    public string CompareValuePayload { get; private set; }
    public int Order { get; private set; }

    private AutomationCondition()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        FieldPath = string.Empty;
        CompareValuePayload = "null";
    }

    private AutomationCondition(
        Guid ruleId,
        AutomationConditionDefinition definition,
        int order)
    {
        if (ruleId == Guid.Empty)
            throw new ArgumentException("RuleId is required.", nameof(ruleId));

        Id = Guid.NewGuid();
        RuleId = ruleId;
        Order = order;

        if (definition.DeviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.EndpointId))
            throw new ArgumentException("EndpointId is required.", nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.CapabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.FieldPath))
            throw new ArgumentException("FieldPath is required.", nameof(definition));

        DeviceId = definition.DeviceId;
        EndpointId = definition.EndpointId.Trim();
        CapabilityId = definition.CapabilityId.Trim();
        FieldPath = definition.FieldPath.Trim();
        Operator = definition.Operator;
        CompareValuePayload = SerializeCompareValue(definition.CompareValue);
    }

    internal static AutomationCondition FromDefinition(
        Guid ruleId,
        AutomationConditionDefinition definition,
        int order)
    {
        return new AutomationCondition(ruleId, definition, order);
    }

    public object? GetCompareValue()
    {
        if (string.IsNullOrWhiteSpace(CompareValuePayload))
            return null;

        using var document = JsonDocument.Parse(CompareValuePayload);
        return JsonPayloadHelper.ConvertJsonElement(document.RootElement);
    }

    private static string SerializeCompareValue(object? compareValue)
    {
        return JsonSerializer.Serialize(compareValue);
    }
}
