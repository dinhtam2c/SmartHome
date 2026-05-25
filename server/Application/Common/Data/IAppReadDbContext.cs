using Core.Domain.DeviceTelemetry;
using Core.Domain.Automations;
using Core.Domain.DeviceCommands;
using Core.Domain.Devices;
using Core.Domain.Floors;
using Core.Domain.Homes;
using Core.Domain.Scenes;

namespace Application.Common.Data;

public interface IAppReadDbContext
{
    IQueryable<Device> Devices { get; }
    IQueryable<AutomationRule> AutomationRules { get; }
    IQueryable<AutomationExecution> AutomationExecutions { get; }
    IQueryable<AutomationExecutionAction> AutomationExecutionActions { get; }
    IQueryable<Floor> Floors { get; }
    IQueryable<Home> Homes { get; }
    IQueryable<Scene> Scenes { get; }
    IQueryable<SceneExecution> SceneExecutions { get; }
    IQueryable<SceneExecutionAction> SceneExecutionActions { get; }
    IQueryable<DeviceCommandExecution> DeviceCommandExecutions { get; }
    IQueryable<DeviceCapabilityStateHistory> DeviceCapabilityStateHistories { get; }
}
