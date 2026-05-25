using Domain.Common;

namespace Domain.Models.ActionSets;

public sealed class ActionSetActionExecution
{
    public Guid Id { get; private set; }
    public Guid ExecutionId { get; private set; }
    public Guid SourceActionId { get; private set; }
    public ActionSetSection Section { get; private set; }
    public ActionType Type { get; private set; }
    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; } = string.Empty;
    public string CapabilityId { get; private set; } = string.Empty;
    public string? Operation { get; private set; }
    public IReadOnlyDictionary<string, object?> State { get; private set; } = EmptyValues();
    public IReadOnlyDictionary<string, object?> Payload { get; private set; } = EmptyValues();
    public int Order { get; private set; }
    public ActionExecutionStatus Status { get; private set; }
    public Guid? DeviceCommandExecutionId { get; private set; }
    public string? Error { get; private set; }

    private ActionSetActionExecution()
    {
    }

    private ActionSetActionExecution(
        Guid executionId,
        ActionSetAction action)
    {
        Id = Guid.NewGuid();
        ExecutionId = executionId;
        SourceActionId = action.Id;
        Section = action.Section;
        Type = action.Type;
        DeviceId = action.DeviceId;
        EndpointId = action.EndpointId;
        CapabilityId = action.CapabilityId;
        Operation = action.Operation;
        State = StructuredValue.SnapshotDictionary(action.State);
        Payload = StructuredValue.SnapshotDictionary(action.Payload);
        Order = action.Order;
        Status = ActionExecutionStatus.Pending;
    }

    internal static ActionSetActionExecution SnapshotFrom(
        Guid executionId,
        ActionSetAction action)
    {
        return new ActionSetActionExecution(executionId, action);
    }

    internal void MarkSkipped(string? reason = null)
    {
        Status = ActionExecutionStatus.Skipped;
        DeviceCommandExecutionId = null;
        Error = reason;
    }

    internal void MarkWaitingForResult(Guid deviceCommandExecutionId)
    {
        if (deviceCommandExecutionId == Guid.Empty)
            throw new ArgumentException("DeviceCommandExecutionId is required.", nameof(deviceCommandExecutionId));

        Status = ActionExecutionStatus.WaitingForResult;
        DeviceCommandExecutionId = deviceCommandExecutionId;
        Error = null;
    }

    internal void MarkSucceeded()
    {
        Status = ActionExecutionStatus.Succeeded;
        Error = null;
    }

    internal void MarkFailed(
        string? error,
        Guid? deviceCommandExecutionId = null,
        bool clearDeviceCommandExecutionId = false)
    {
        Status = ActionExecutionStatus.Failed;
        DeviceCommandExecutionId = clearDeviceCommandExecutionId
            ? null
            : deviceCommandExecutionId ?? DeviceCommandExecutionId;
        Error = string.IsNullOrWhiteSpace(error) ? "Action failed" : error;
    }

    private static Dictionary<string, object?> EmptyValues()
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}
