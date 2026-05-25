using Domain.Common;

namespace Domain.Models.ActionSets;

public sealed class ActionSetAction
{
    public Guid Id { get; private set; }
    public Guid ActionSetId { get; private set; }
    public ActionSetSection Section { get; private set; }
    public ActionType Type { get; private set; }
    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; } = string.Empty;
    public string CapabilityId { get; private set; } = string.Empty;
    public string? Operation { get; private set; }
    public IReadOnlyDictionary<string, object?> State { get; private set; } = EmptyValues();
    public IReadOnlyDictionary<string, object?> Payload { get; private set; } = EmptyValues();
    public int Order { get; private set; }

    private ActionSetAction()
    {
    }

    private ActionSetAction(
        Guid actionSetId,
        ActionSetSection section,
        ActionDefinition definition,
        int order)
    {
        if (actionSetId == Guid.Empty)
            throw new ArgumentException("ActionSetId is required.", nameof(actionSetId));

        ArgumentNullException.ThrowIfNull(definition);
        ValidateTarget(definition.Target);

        Id = Guid.NewGuid();
        ActionSetId = actionSetId;
        Section = section;
        Type = definition.Type;
        DeviceId = definition.Target.DeviceId;
        EndpointId = definition.Target.EndpointId.Trim();
        CapabilityId = definition.Target.CapabilityId.Trim();
        Order = order;

        switch (definition)
        {
            case SetStateActionDefinition setState:
                if (setState.State.Count == 0)
                    throw new InvalidOperationException("Set-state action state must contain at least one field.");

                State = StructuredValue.SnapshotDictionary(setState.State);
                break;
            case InvokeOperationActionDefinition invokeOperation:
                Operation = NormalizeOperation(invokeOperation.Operation);
                Payload = StructuredValue.SnapshotDictionary(invokeOperation.Payload);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(definition),
                    definition.Type,
                    "Unsupported action type.");
        }
    }

    internal static ActionSetAction FromDefinition(
        Guid actionSetId,
        ActionSetSection section,
        ActionDefinition definition,
        int order)
    {
        return new ActionSetAction(actionSetId, section, definition, order);
    }

    public ActionDefinition ToDefinition()
    {
        var target = new ActionTarget(DeviceId, EndpointId, CapabilityId);
        return Type switch
        {
            ActionType.SetState => new SetStateActionDefinition(
                target,
                StructuredValue.SnapshotDictionary(State)),
            ActionType.InvokeOperation => new InvokeOperationActionDefinition(
                target,
                Operation ?? string.Empty,
                StructuredValue.SnapshotDictionary(Payload)),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unsupported action type.")
        };
    }

    private static Dictionary<string, object?> EmptyValues()
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateTarget(ActionTarget target)
    {
        if (target.DeviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(target));

        if (string.IsNullOrWhiteSpace(target.EndpointId))
            throw new ArgumentException("EndpointId is required.", nameof(target));

        if (string.IsNullOrWhiteSpace(target.CapabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(target));
    }

    private static string NormalizeOperation(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required.", nameof(operation));

        return operation.Trim();
    }
}
