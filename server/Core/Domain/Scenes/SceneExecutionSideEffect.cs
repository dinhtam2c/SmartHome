using System.Text.Json;
using Core.Common;

namespace Core.Domain.Scenes;

public class SceneExecutionSideEffect
{
    public Guid Id { get; private set; }
    public Guid SceneExecutionId { get; private set; }
    public Guid SceneSideEffectId { get; private set; }

    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string Operation { get; private set; }
    public string ParamsPayload { get; private set; }
    public SceneSideEffectTiming Timing { get; private set; }
    public int DelayMs { get; private set; }
    public int Order { get; private set; }

    public SceneExecutionSideEffectStatus Status { get; private set; }
    public string? CommandCorrelationId { get; private set; }
    public string? Error { get; private set; }
    public long UpdatedAt { get; private set; }

    private SceneExecutionSideEffect()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        Operation = string.Empty;
        ParamsPayload = "{}";
    }

    private SceneExecutionSideEffect(
        Guid sceneExecutionId,
        SceneSideEffect sideEffect,
        int order,
        long now)
    {
        Id = Guid.NewGuid();
        SceneExecutionId = sceneExecutionId;
        SceneSideEffectId = sideEffect.Id;
        DeviceId = sideEffect.DeviceId;
        EndpointId = sideEffect.EndpointId;
        CapabilityId = sideEffect.CapabilityId;
        Operation = sideEffect.Operation;
        ParamsPayload = sideEffect.ParamsPayload;
        Timing = sideEffect.Timing;
        DelayMs = sideEffect.DelayMs;
        Order = order;

        Status = SceneExecutionSideEffectStatus.Pending;
        UpdatedAt = now;
    }

    internal static SceneExecutionSideEffect SnapshotFrom(
        Guid sceneExecutionId,
        SceneSideEffect sideEffect,
        int order,
        long now)
    {
        return new SceneExecutionSideEffect(sceneExecutionId, sideEffect, order, now);
    }

    public Dictionary<string, object?> GetParams()
    {
        if (string.IsNullOrWhiteSpace(ParamsPayload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(ParamsPayload);
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

    internal void MarkSucceeded(string? correlationId, long? updatedAt = null)
    {
        Status = SceneExecutionSideEffectStatus.Succeeded;
        CommandCorrelationId = correlationId;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkFailed(string? error, string? correlationId = null, long? updatedAt = null)
    {
        Status = SceneExecutionSideEffectStatus.Failed;
        CommandCorrelationId = correlationId;
        Error = error;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkSkipped(string? reason = null, long? updatedAt = null)
    {
        Status = SceneExecutionSideEffectStatus.Skipped;
        CommandCorrelationId = null;
        Error = reason;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

}

public enum SceneExecutionSideEffectStatus
{
    Pending,
    Succeeded,
    Failed,
    Skipped
}
