using Application.Commands.Scenes;
using Application.Commands.Scenes.CreateScene;
using Application.Commands.Scenes.DeleteScene;
using Application.Commands.Scenes.ExecuteScene;
using Application.Commands.Scenes.UpdateScene;
using Application.Queries.Scenes.GetSceneDetails;
using Application.Queries.Scenes.GetSceneExecutionDetails;
using Application.Queries.Scenes.GetSceneExecutions;
using Application.Queries.Scenes.GetScenes;
using Core.Domain.Scenes;
using MediatR;

namespace WebAPI.Scenes;

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
        sceneApi.MapGet("/{sceneId}/executions", GetSceneExecutions);
        sceneApi.MapGet("/{sceneId}/executions/{executionId}", GetSceneExecutionDetails);
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
            request.Targets?.Select(ToTargetModel),
            request.SideEffects?.Select(ToSideEffectModel));

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
            request.Targets?.Select(ToTargetModel),
            request.SideEffects?.Select(ToSideEffectModel));

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
        ExecuteSceneRequest? request,
        ISender sender,
        CancellationToken ct)
    {
        var executionId = await sender.Send(
            new ExecuteSceneCommand(
                homeId,
                sceneId,
                request?.TriggerSource,
                request is null
                    ? null
                    : new ExecuteSceneOptions(
                        request.OnlyEndpoints,
                        request.ExcludeCapabilities)),
            ct);

        return Results.Accepted(
            $"/homes/{homeId}/scenes/{sceneId}/executions/{executionId}",
            new { executionId });
    }

    private static async Task<IResult> GetSceneExecutions(
        Guid homeId,
        Guid sceneId,
        ISender sender,
        CancellationToken ct,
        SceneExecutionStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(
            new GetSceneExecutionsQuery(homeId, sceneId, status, page, pageSize),
            ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetSceneExecutionDetails(
        Guid homeId,
        Guid sceneId,
        Guid executionId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GetSceneExecutionDetailsQuery(homeId, sceneId, executionId),
            ct);

        return Results.Ok(result);
    }

    private static SceneTargetModel ToTargetModel(SceneTargetRequest target)
    {
        return new SceneTargetModel(
            target.DeviceId,
            target.EndpointId,
            target.CapabilityId,
            target.DesiredState);
    }

    private static SceneSideEffectModel ToSideEffectModel(SceneSideEffectRequest sideEffect)
    {
        return new SceneSideEffectModel(
            sideEffect.DeviceId,
            sideEffect.EndpointId,
            sideEffect.CapabilityId,
            sideEffect.Operation,
            sideEffect.Params,
            sideEffect.Timing,
            sideEffect.DelayMs);
    }
}
