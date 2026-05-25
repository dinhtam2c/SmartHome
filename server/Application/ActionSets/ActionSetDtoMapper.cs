using Core.Domain.ActionSets;
using Core.Domain.Automations;
using Core.Domain.Scenes;

namespace Application.ActionSets;

public static class ActionSetDtoMapper
{
    public static ActionSetDto ForScene(Scene scene)
    {
        return new ActionSetDto(
            scene.Actions
                .Where(action => action.Section == ActionSetSection.Main)
                .OrderBy(action => action.Order)
                .Select(ToDto)
                .ToList(),
            new ActionSetHooksDto(
                scene.Actions
                    .Where(action => action.Section == ActionSetSection.Before)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList(),
                scene.Actions
                    .Where(action => action.Section == ActionSetSection.OnSuccess)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList(),
                scene.Actions
                    .Where(action => action.Section == ActionSetSection.OnFailure)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList()),
            new ActionSetExecutionPolicyDto(
                scene.ExecutionMode.ToWireName(),
                scene.ContinueOnError));
    }

    public static ActionSetDto ForAutomationRule(AutomationRule rule)
    {
        return new ActionSetDto(
            rule.Actions
                .Where(action => action.Section == ActionSetSection.Main)
                .OrderBy(action => action.Order)
                .Select(ToDto)
                .ToList(),
            new ActionSetHooksDto(
                rule.Actions
                    .Where(action => action.Section == ActionSetSection.Before)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList(),
                rule.Actions
                    .Where(action => action.Section == ActionSetSection.OnSuccess)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList(),
                rule.Actions
                    .Where(action => action.Section == ActionSetSection.OnFailure)
                    .OrderBy(action => action.Order)
                    .Select(ToDto)
                    .ToList()),
            new ActionSetExecutionPolicyDto(
                rule.ExecutionMode.ToWireName(),
                rule.ContinueOnError));
    }

    public static ActionSetActionDto ToDto(SceneAction action)
    {
        return new ActionSetActionDto(
            action.Id,
            action.Type.ToWireName(),
            new ActionTargetDto(action.DeviceId, action.EndpointId, action.CapabilityId),
            action.Type == ActionType.SetState ? action.GetState() : null,
            action.Type == ActionType.SetState ? action.GetOptions() : null,
            action.Type == ActionType.InvokeOperation ? action.Operation : null,
            action.Type == ActionType.InvokeOperation ? action.GetPayload() : null,
            action.Order);
    }

    public static ActionSetActionDto ToDto(AutomationAction action)
    {
        return new ActionSetActionDto(
            action.Id,
            action.Type.ToWireName(),
            new ActionTargetDto(action.DeviceId, action.EndpointId, action.CapabilityId),
            action.Type == ActionType.SetState ? action.GetState() : null,
            action.Type == ActionType.SetState ? action.GetOptions() : null,
            action.Type == ActionType.InvokeOperation ? action.Operation : null,
            action.Type == ActionType.InvokeOperation ? action.GetPayload() : null,
            action.Order);
    }

    public static ActionExecutionDto ToDto(SceneExecutionAction action)
    {
        return new ActionExecutionDto(
            action.Id,
            action.SceneActionId,
            action.Section.ToWireName(),
            action.Type.ToWireName(),
            new ActionTargetDto(action.DeviceId, action.EndpointId, action.CapabilityId),
            action.Type == ActionType.SetState ? action.GetState() : null,
            action.Type == ActionType.SetState ? action.GetOptions() : null,
            action.Type == ActionType.InvokeOperation ? action.Operation : null,
            action.Type == ActionType.InvokeOperation ? action.GetPayload() : null,
            action.Status.ToWireName(),
            action.CommandCorrelationId,
            action.GetUnresolvedDiff(),
            action.Error,
            action.Order,
            action.UpdatedAt);
    }

    public static ActionExecutionDto ToDto(AutomationExecutionAction action)
    {
        return new ActionExecutionDto(
            action.Id,
            action.AutomationActionId,
            action.Section.ToWireName(),
            action.Type.ToWireName(),
            new ActionTargetDto(action.DeviceId, action.EndpointId, action.CapabilityId),
            action.Type == ActionType.SetState ? action.GetState() : null,
            action.Type == ActionType.SetState ? action.GetOptions() : null,
            action.Type == ActionType.InvokeOperation ? action.Operation : null,
            action.Type == ActionType.InvokeOperation ? action.GetPayload() : null,
            action.Status.ToWireName(),
            action.CommandCorrelationId,
            action.GetUnresolvedDiff(),
            action.Error,
            action.Order,
            action.UpdatedAt);
    }
}
