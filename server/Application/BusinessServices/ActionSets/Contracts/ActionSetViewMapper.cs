using Domain.Models.ActionSets;
using Domain.Models.Scenes;
using Domain.Models.Automations;

namespace Application.BusinessServices.ActionSets.Contracts;

public static class ActionSetViewMapper
{
    public static ActionSetView ForScene(Scene scene)
    {
        return new ActionSetView(
            scene.ActionSet.MainActions.Select(ToView).ToList(),
            new ActionSetHooksView(
                scene.ActionSet.BeforeActions.Select(ToView).ToList(),
                scene.ActionSet.OnSuccessActions.Select(ToView).ToList(),
                scene.ActionSet.OnFailureActions.Select(ToView).ToList()),
            new ActionSetExecutionPolicyView(
                scene.ActionSet.ExecutionMode.ToWireName(),
                scene.ActionSet.ContinueOnError));
    }

    public static ActionSetView ForAutomationRule(AutomationRule rule)
    {
        return new ActionSetView(
            rule.ActionSet.MainActions.Select(ToView).ToList(),
            new ActionSetHooksView(
                rule.ActionSet.BeforeActions.Select(ToView).ToList(),
                rule.ActionSet.OnSuccessActions.Select(ToView).ToList(),
                rule.ActionSet.OnFailureActions.Select(ToView).ToList()),
            new ActionSetExecutionPolicyView(
                rule.ActionSet.ExecutionMode.ToWireName(),
                rule.ActionSet.ContinueOnError));
    }

    public static ActionSetActionView ToView(ActionSetAction action)
    {
        return new ActionSetActionView(
            action.Id,
            action.Type.ToWireName(),
            new ActionTargetView(action.DeviceId, action.EndpointId, action.CapabilityId),
            action.Type == ActionType.SetState ? action.State : null,
            action.Type == ActionType.InvokeOperation ? action.Operation : null,
            action.Type == ActionType.InvokeOperation ? action.Payload : null,
            action.Order);
    }
}
