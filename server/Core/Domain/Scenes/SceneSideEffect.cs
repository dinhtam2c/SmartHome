using System.Text.Json;
using Core.Common;

namespace Core.Domain.Scenes;

public class SceneSideEffect
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }

    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string Operation { get; private set; }
    public string ParamsPayload { get; private set; }
    public SceneSideEffectTiming Timing { get; private set; }
    public int DelayMs { get; private set; }
    public int Order { get; private set; }

    private SceneSideEffect()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        Operation = string.Empty;
        ParamsPayload = "{}";
    }

    private SceneSideEffect(
        Guid sceneId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        string operation,
        Dictionary<string, object?> parameters,
        SceneSideEffectTiming timing,
        int delayMs,
        int order)
    {
        if (sceneId == Guid.Empty)
            throw new ArgumentException("SceneId is required.", nameof(sceneId));

        if (deviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        if (string.IsNullOrWhiteSpace(endpointId))
            throw new ArgumentException("EndpointId is required.", nameof(endpointId));

        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(capabilityId));

        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required.", nameof(operation));

        if (delayMs < 0)
            throw new ArgumentException("DelayMs must be >= 0.", nameof(delayMs));

        Id = Guid.NewGuid();
        SceneId = sceneId;
        DeviceId = deviceId;
        EndpointId = endpointId.Trim();
        CapabilityId = capabilityId.Trim();
        Operation = operation.Trim();
        ParamsPayload = SerializeParams(parameters);
        Timing = timing;
        DelayMs = delayMs;
        Order = order;
    }

    internal static SceneSideEffect FromDefinition(
        Guid sceneId,
        SceneSideEffectDefinition definition,
        int order)
    {
        return new SceneSideEffect(
            sceneId,
            definition.DeviceId,
            definition.EndpointId,
            definition.CapabilityId,
            definition.Operation,
            definition.Params,
            definition.Timing,
            definition.DelayMs,
            order);
    }

    public Dictionary<string, object?> GetParams()
    {
        return DeserializeParams(ParamsPayload);
    }

    private static string SerializeParams(Dictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return JsonSerializer.Serialize(parameters);
    }

    private static Dictionary<string, object?> DeserializeParams(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("SideEffect params payload must be a JSON object.");
        }

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = JsonPayloadHelper.ConvertJsonElement(property.Value);
        }

        return dictionary;
    }
}
