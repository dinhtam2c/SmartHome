using Application.BusinessServices.ActionSets.Contracts;
using Application.BusinessServices.Automations.Rules;
using Application.UseCases.Automations.CreateAutomationRule;
using Application.UseCases.Automations.DeleteAutomationRule;
using Application.UseCases.Automations.ExecuteAutomationRule;
using Application.UseCases.Automations.UpdateAutomationRule;
using Application.UseCases.Automations.GetAutomationRuleDetails;
using Application.UseCases.Automations.GetAutomationRules;
using MediatR;
using Presentation.ActionSets;

namespace Presentation.Automations;

public static class AutomationEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var automationApi = routes.MapGroup("/homes/{homeId}/automations");

        automationApi.MapGet("/", GetAutomationRules);
        automationApi.MapGet("/{ruleId}", GetAutomationRuleDetails);
        automationApi.MapPost("/", CreateAutomationRule);
        automationApi.MapPatch("/{ruleId}", UpdateAutomationRule);
        automationApi.MapDelete("/{ruleId}", DeleteAutomationRule);

        automationApi.MapPost("/{ruleId}/execute", ExecuteAutomationRule);
    }

    private static async Task<IResult> GetAutomationRules(
        Guid homeId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAutomationRulesQuery(homeId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAutomationRuleDetails(
        Guid homeId,
        Guid ruleId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAutomationRuleDetailsQuery(homeId, ruleId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAutomationRule(
        Guid homeId,
        AddAutomationRuleRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var ruleId = await sender.Send(
            new CreateAutomationRuleCommand(
                homeId,
                request.Name,
                request.Description,
                request.IsEnabled,
                request.ConditionLogic,
                request.CooldownMs,
                request.Conditions?.Select(ToConditionModel),
                ToTimeWindowModel(request.TimeWindow),
                ToActionSetInput(request.ActionSet)),
            ct);

        return Results.Created($"/homes/{homeId}/automations/{ruleId}", new { id = ruleId });
    }

    private static async Task<IResult> UpdateAutomationRule(
        Guid homeId,
        Guid ruleId,
        UpdateAutomationRuleRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new UpdateAutomationRuleCommand(
                homeId,
                ruleId,
                request.Name,
                request.Description,
                request.IsEnabled,
                request.ConditionLogic,
                request.CooldownMs,
                request.Conditions?.Select(ToConditionModel),
                ToTimeWindowModel(request.TimeWindow),
                ToActionSetInput(request.ActionSet)),
            ct);

        return Results.Ok();
    }

    private static async Task<IResult> DeleteAutomationRule(
        Guid homeId,
        Guid ruleId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteAutomationRuleCommand(homeId, ruleId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ExecuteAutomationRule(
        Guid homeId,
        Guid ruleId,
        ISender sender,
        CancellationToken ct)
    {
        var executionId = await sender.Send(
            new ExecuteAutomationRuleCommand(homeId, ruleId),
            ct);

        return Results.Accepted(value: new { executionId });
    }

    private static AutomationConditionModel ToConditionModel(AutomationConditionRequest condition)
    {
        return new AutomationConditionModel(
            condition.DeviceId,
            condition.EndpointId,
            condition.CapabilityId,
            condition.FieldPath,
            condition.Operator,
            condition.CompareValue);
    }

    private static AutomationTimeWindowModel? ToTimeWindowModel(AutomationTimeWindowRequest? timeWindow)
    {
        return timeWindow is null
            ? null
            : new AutomationTimeWindowModel(
                timeWindow.Enabled,
                timeWindow.StartTime,
                timeWindow.EndTime,
                timeWindow.DaysOfWeek);
    }

    private static ActionSetInput? ToActionSetInput(ActionSetRequest? actionSet)
    {
        return actionSet is null
            ? null
            : new ActionSetInput(
                actionSet.Actions?.Select(ToActionModel),
                actionSet.Hooks is null
                    ? null
                    : new ActionSetHooksInput(
                        actionSet.Hooks.Before?.Select(ToActionModel),
                        actionSet.Hooks.OnSuccess?.Select(ToActionModel),
                        actionSet.Hooks.OnFailure?.Select(ToActionModel)),
                actionSet.ExecutionPolicy is null
                    ? null
                    : new ActionSetExecutionPolicyInput(
                        actionSet.ExecutionPolicy.Mode,
                        actionSet.ExecutionPolicy.ContinueOnError));
    }

    private static ActionSetActionInput ToActionModel(ActionRequest action)
    {
        return new ActionSetActionInput(
            action.Type,
            action.Target is null
                ? null
                : new ActionTargetInput(
                    action.Target.DeviceId,
                    action.Target.EndpointId,
                    action.Target.CapabilityId),
            action.State,
            action.Operation,
            action.Payload);
    }
}
