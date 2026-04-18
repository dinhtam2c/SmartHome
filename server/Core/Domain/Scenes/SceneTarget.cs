using System.Text.Json;
using Core.Common;

namespace Core.Domain.Scenes;

public class SceneTarget
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }

    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string DesiredStatePayload { get; private set; }
    public int Order { get; private set; }

    private SceneTarget()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        DesiredStatePayload = "{}";
    }

    private SceneTarget(
        Guid sceneId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        Dictionary<string, object?> desiredState,
        int order)
    {
        if (sceneId == Guid.Empty)
            throw new ArgumentException("SceneId is required.", nameof(sceneId));

        if (deviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(capabilityId));

        if (string.IsNullOrWhiteSpace(endpointId))
            throw new ArgumentException("EndpointId is required.", nameof(endpointId));

        Id = Guid.NewGuid();
        SceneId = sceneId;
        DeviceId = deviceId;
        EndpointId = endpointId.Trim();
        CapabilityId = capabilityId.Trim();
        DesiredStatePayload = SerializeDesiredState(desiredState);
        Order = order;
    }

    internal static SceneTarget FromTargetDefinition(Guid sceneId, SceneTargetDefinition definition, int order)
    {
        return new SceneTarget(
            sceneId,
            definition.DeviceId,
            definition.EndpointId,
            definition.CapabilityId,
            definition.DesiredState,
            order);
    }

    public Dictionary<string, object?> GetDesiredState()
    {
        return DeserializeDesiredState(DesiredStatePayload);
    }

    private static string SerializeDesiredState(Dictionary<string, object?> desiredState)
    {
        if (desiredState is null)
            throw new ArgumentNullException(nameof(desiredState));

        if (desiredState.Count == 0)
            throw new InvalidOperationException("DesiredState must contain at least one field.");

        return JsonSerializer.Serialize(desiredState);
    }

    private static Dictionary<string, object?> DeserializeDesiredState(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("DesiredState payload must be a JSON object.");
        }

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = JsonPayloadHelper.ConvertJsonElement(property.Value);
        }

        return dictionary;
    }
}
