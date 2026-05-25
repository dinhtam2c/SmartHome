using Application.BusinessServices.ActionSets.Contracts;
using Application.UseCases.Scenes.CreateScene;
using Application.UseCases.Scenes.DeleteScene;
using Application.UseCases.Scenes.ExecuteScene;
using Application.UseCases.Scenes.GetSceneDetails;
using Application.UseCases.Scenes.GetScenes;
using Application.UseCases.Scenes.UpdateScene;
using MediatR;
using Presentation.ActionSets;

namespace Presentation.Scenes;

public static class SceneEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var sceneApi = routes.MapGroup("/homes/{homeId}/scenes");

        sceneApi.MapGet("/", GetScenes);
        sceneApi.MapGet("/{sceneId}", GetSceneDetails);
        sceneApi.MapPost("/", CreateScene);
        sceneApi.MapPatch("/{sceneId}", UpdateScene);
        sceneApi.MapDelete("/{sceneId}", DeleteScene);

        sceneApi.MapPost("/{sceneId}/execute", ExecuteScene);
    }

    private static async Task<IResult> GetScenes(
        Guid homeId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetScenesQuery(homeId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSceneDetails(
        Guid homeId,
        Guid sceneId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetSceneDetailsQuery(homeId, sceneId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateScene(
        Guid homeId,
        AddSceneRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateSceneCommand(
            homeId,
            request.Name,
            request.Description,
            request.IsEnabled,
            ToActionSetInput(request.ActionSet));

        var sceneId = await sender.Send(command, ct);
        return Results.Created($"/homes/{homeId}/scenes/{sceneId}", new { id = sceneId });
    }

    private static async Task<IResult> UpdateScene(
        Guid homeId,
        Guid sceneId,
        UpdateSceneRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateSceneCommand(
            homeId,
            sceneId,
            request.Name,
            request.Description,
            request.IsEnabled,
            ToActionSetInput(request.ActionSet));

        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteScene(
        Guid homeId,
        Guid sceneId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteSceneCommand(homeId, sceneId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ExecuteScene(
        Guid homeId,
        Guid sceneId,
        ISender sender,
        CancellationToken ct)
    {
        var executionId = await sender.Send(
            new ExecuteSceneCommand(homeId, sceneId),
            ct);

        return Results.Accepted(value: new { executionId });
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
