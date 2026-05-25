using Application.BusinessServices.ActionSets.Planning;
using Application.BusinessServices.Devices.State;
using Domain.Models.ActionSets;
using Domain.Models.Devices.Commands;

namespace Application.BusinessServices.ActionSets.Execution;

internal static class ActionOutcomeResolver
{
    public static void ApplyCommandResult(
        ActionSetExecution execution,
        Guid deviceCommandExecutionId,
        CommandLifecycleStatus commandStatus,
        IReadOnlyCollection<CapabilityStateUpdate> stateChanges,
        string? commandError)
    {
        var action = execution.FindActionByDeviceCommandExecutionId(deviceCommandExecutionId);
        if (action is null)
            return;

        var error = GetFailure(action, commandStatus, stateChanges, commandError);
        if (error is null)
            execution.MarkActionSucceeded(action.Id);
        else
            execution.MarkActionFailed(action.Id, error);
    }

    private static string? GetFailure(
        ActionSetActionExecution action,
        CommandLifecycleStatus commandStatus,
        IReadOnlyCollection<CapabilityStateUpdate> stateChanges,
        string? commandError)
    {
        if (commandStatus == CommandLifecycleStatus.Failed)
            return string.IsNullOrWhiteSpace(commandError) ? "Command failed" : commandError;

        if (commandStatus != CommandLifecycleStatus.Completed)
            return $"Unsupported command outcome '{commandStatus}'.";

        if (action.Type == ActionType.InvokeOperation)
            return null;

        if (action.Type != ActionType.SetState)
            return $"Unsupported action type '{action.Type}'.";

        var desiredState = ActionStateHelper.NormalizeState(action.State);
        var stateChange = stateChanges.FirstOrDefault(change =>
            change.CapabilityId.Equals(action.CapabilityId, StringComparison.OrdinalIgnoreCase)
            && change.EndpointId.Equals(action.EndpointId, StringComparison.OrdinalIgnoreCase));

        return stateChange is not null
               && ActionStateHelper.AreEquivalent(stateChange.State, desiredState)
            ? null
            : "Verification failed: command result state does not match desired state.";
    }
}
