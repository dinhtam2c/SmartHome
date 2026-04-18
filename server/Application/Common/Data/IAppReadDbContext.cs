using Core.Domain.Data;
using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Core.Domain.Homes;
using Core.Domain.Scenes;

namespace Application.Common.Data;

public interface IAppReadDbContext
{
    IQueryable<Device> Devices { get; }
    IQueryable<Home> Homes { get; }
    IQueryable<Scene> Scenes { get; }
    IQueryable<SceneExecution> SceneExecutions { get; }
    IQueryable<SceneExecutionTarget> SceneExecutionTargets { get; }
    IQueryable<SceneSideEffect> SceneSideEffects { get; }
    IQueryable<SceneExecutionSideEffect> SceneExecutionSideEffects { get; }
    IQueryable<DeviceCommandExecution> DeviceCommandExecutions { get; }
    IQueryable<DeviceCapabilityStateHistory> DeviceCapabilityStateHistories { get; }
}
