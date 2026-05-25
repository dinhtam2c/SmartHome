using Application.BusinessServices.ActionSets.Contracts;
using Application.Common.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Scenes;

namespace Application.BusinessServices.Scenes.Realtime;

public static class SceneRealtime
{
    public static RealtimeDelta ForScene(Scene scene, string change)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.Scene,
            change: change,
            homeId: scene.HomeId,
            sceneId: scene.Id,
            delta: new
            {
                scene.Id,
                scene.HomeId,
                scene.Name,
                scene.Description,
                scene.IsEnabled,
                MainActionCount = scene.ActionSet.Actions.Count(action => action.Section == ActionSetSection.Main),
                HookActionCount = scene.ActionSet.Actions.Count(action => action.Section != ActionSetSection.Main),
                scene.UpdatedAt
            });
    }

    public static RealtimeDelta ForDeleted(Guid homeId, Guid sceneId)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.Scene,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            sceneId: sceneId);
    }

    public static RealtimeDelta ForExecution(ActionSetExecution execution)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.SceneExecution,
            change: RealtimeChanges.Updated,
            homeId: execution.HomeId,
            sceneId: execution.SourceId,
            executionId: execution.Id,
            delta: new
            {
                execution.Id,
                SceneId = execution.SourceId,
                execution.HomeId,
                execution.Status,
                Phase = execution.Phase.ToWireName(),
                execution.StartedAt,
                execution.FinishedAt
            });
    }
}
