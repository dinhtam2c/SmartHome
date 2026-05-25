using Core.Common;
using Core.Domain.ActionSets;

namespace Core.Domain.Scenes;

public class SceneAction
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public ActionSetSection Section { get; private set; }
    public ActionType Type { get; private set; }
    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string? Operation { get; private set; }
    public string StatePayload { get; private set; }
    public string OptionsPayload { get; private set; }
    public string Payload { get; private set; }
    public int Order { get; private set; }

    private SceneAction()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        StatePayload = "{}";
        OptionsPayload = "{}";
        Payload = "{}";
    }

    private SceneAction(
        Guid sceneId,
        ActionSetSection section,
        ActionDefinition definition,
        int order)
    {
        if (sceneId == Guid.Empty)
            throw new ArgumentException("SceneId is required.", nameof(sceneId));

        if (definition.Target.DeviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.Target.EndpointId))
            throw new ArgumentException("EndpointId is required.", nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.Target.CapabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(definition));

        Id = Guid.NewGuid();
        SceneId = sceneId;
        Section = section;
        Type = definition.Type;
        DeviceId = definition.Target.DeviceId;
        EndpointId = definition.Target.EndpointId.Trim();
        CapabilityId = definition.Target.CapabilityId.Trim();
        Order = order;
        StatePayload = "{}";
        OptionsPayload = "{}";
        Payload = "{}";

        switch (definition)
        {
            case SetStateActionDefinition setState:
                StatePayload = JsonPayloadHelper.SerializeDictionary(
                    setState.State,
                    "Scene set-state action state",
                    requireNonEmpty: true);
                OptionsPayload = JsonPayloadHelper.SerializeDictionary(
                    setState.Options,
                    "Scene set-state action options");
                break;
            case InvokeOperationActionDefinition invoke:
                if (string.IsNullOrWhiteSpace(invoke.Operation))
                    throw new ArgumentException("Operation is required.", nameof(definition));

                Operation = invoke.Operation.Trim();
                Payload = JsonPayloadHelper.SerializeDictionary(
                    invoke.Payload,
                    "Scene invoke-operation action payload");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(definition), definition.Type, "Unsupported action type.");
        }
    }

    internal static SceneAction FromDefinition(
        Guid sceneId,
        ActionSetSection section,
        ActionDefinition definition,
        int order)
    {
        return new SceneAction(sceneId, section, definition, order);
    }

    public ActionDefinition ToDefinition()
    {
        var target = new ActionTarget(DeviceId, EndpointId, CapabilityId);

        return Type switch
        {
            ActionType.SetState => new SetStateActionDefinition(
                target,
                GetState(),
                GetOptions()),
            ActionType.InvokeOperation => new InvokeOperationActionDefinition(
                target,
                Operation ?? string.Empty,
                GetPayload()),
            _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unsupported action type.")
        };
    }

    public Dictionary<string, object?> GetState()
    {
        return JsonPayloadHelper.DeserializeDictionary(StatePayload, "Scene set-state action state");
    }

    public Dictionary<string, object?> GetOptions()
    {
        return JsonPayloadHelper.DeserializeDictionary(
            OptionsPayload,
            "Scene set-state action options",
            throwOnNonObject: false);
    }

    public Dictionary<string, object?> GetPayload()
    {
        return JsonPayloadHelper.DeserializeDictionary(
            Payload,
            "Scene invoke-operation action payload",
            throwOnNonObject: false);
    }
}
