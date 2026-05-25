using Application.ActionSets;
using Application.Automations.Rules;
using Application.Commands.Automations.CreateAutomationRule;
using Application.Commands.Automations.DeleteAutomationRule;
using Application.Commands.Automations.ExecuteAutomationRule;
using Application.Commands.Automations.UpdateAutomationRule;
using Application.Queries.Automations.GetAutomationExecutionDetails;
using Application.Queries.Automations.GetAutomationExecutions;
using Application.Queries.Automations.GetAutomationRuleDetails;
using Application.Queries.Automations.GetAutomationRules;
using Core.Domain.Automations;
using MediatR;
using WebAPI.ActionSets;

namespace WebAPI.Automations;

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
        automationApi.MapGet("/{ruleId}/executions", GetAutomationExecutions);
        automationApi.MapGet("/{ruleId}/executions/{executionId}", GetAutomationExecutionDetails);
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
                ToActionSetModel(request.ActionSet)),
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
                ToActionSetModel(request.ActionSet)),
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
        ExecuteAutomationRuleRequest? request,
        ISender sender,
        CancellationToken ct)
    {
        var executionId = await sender.Send(
            new ExecuteAutomationRuleCommand(
                homeId,
                ruleId,
                new AutomationTriggerContext(
                    request?.TriggerDeviceId,
                    request?.TriggerEndpointId,
                    request?.TriggerCapabilityId,
                    request?.TriggerState,
                    request?.TriggerSource ?? "manual")),
            ct);

        return Results.Accepted(
            $"/homes/{homeId}/automations/{ruleId}/executions/{executionId}",
            new { executionId });
    }

    private static async Task<IResult> GetAutomationExecutions(
        Guid homeId,
        Guid ruleId,
        ISender sender,
        CancellationToken ct,
        AutomationExecutionStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(
            new GetAutomationExecutionsQuery(homeId, ruleId, status, page, pageSize),
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAutomationExecutionDetails(
        Guid homeId,
        Guid ruleId,
        Guid executionId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GetAutomationExecutionDetailsQuery(homeId, ruleId, executionId),
            ct);

        return Results.Ok(result);
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

    private static ActionSetModel? ToActionSetModel(ActionSetRequest? actionSet)
    {
        return actionSet is null
            ? null
            : new ActionSetModel(
                actionSet.Actions?.Select(ToActionModel),
                actionSet.Hooks is null
                    ? null
                    : new ActionSetHooksModel(
                        actionSet.Hooks.Before?.Select(ToActionModel),
                        actionSet.Hooks.OnSuccess?.Select(ToActionModel),
                        actionSet.Hooks.OnFailure?.Select(ToActionModel)),
                actionSet.ExecutionPolicy is null
                    ? null
                    : new ActionSetExecutionPolicyModel(
                        actionSet.ExecutionPolicy.Mode,
                        actionSet.ExecutionPolicy.ContinueOnError));
    }

    private static ActionSetActionModel ToActionModel(ActionRequest action)
    {
        return new ActionSetActionModel(
            action.Type,
            action.Target is null
                ? null
                : new ActionTargetModel(
                    action.Target.DeviceId,
                    action.Target.EndpointId,
                    action.Target.CapabilityId),
            action.State,
            action.Options,
            action.Operation,
            action.Payload);
    }
}
