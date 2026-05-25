using System.Text.Json.Serialization;
using WebAPI.Automations;
using WebAPI.ActionSets;
using WebAPI.Devices;
using WebAPI.Floors;
using WebAPI.Homes;
using WebAPI.Rooms;
using WebAPI.Scenes;

namespace WebAPI;

[JsonSerializable(typeof(AddHomeRequest))]
[JsonSerializable(typeof(UpdateHomeRequest))]

[JsonSerializable(typeof(AddRoomRequest))]
[JsonSerializable(typeof(UpdateRoomRequest))]

[JsonSerializable(typeof(AddDeviceRequest))]
[JsonSerializable(typeof(UpdateDeviceInfoRequest))]
[JsonSerializable(typeof(AssignRoomToDeviceRequest))]

[JsonSerializable(typeof(CreateFloorRequest))]
[JsonSerializable(typeof(UpdateFloorInfoRequest))]
[JsonSerializable(typeof(ReorderFloorsRequest))]
[JsonSerializable(typeof(FloorPointRequest))]
[JsonSerializable(typeof(UpsertFloorRoomRequest))]
[JsonSerializable(typeof(PlaceDeviceRequest))]
[JsonSerializable(typeof(MoveDeviceRequest))]

[JsonSerializable(typeof(AddSceneRequest))]
[JsonSerializable(typeof(UpdateSceneRequest))]
[JsonSerializable(typeof(ExecuteSceneRequest))]
[JsonSerializable(typeof(ActionSetRequest))]
[JsonSerializable(typeof(ActionSetHooksRequest))]
[JsonSerializable(typeof(ActionSetExecutionPolicyRequest))]
[JsonSerializable(typeof(ActionTargetRequest))]
[JsonSerializable(typeof(ActionRequest))]

[JsonSerializable(typeof(AddAutomationRuleRequest))]
[JsonSerializable(typeof(UpdateAutomationRuleRequest))]
[JsonSerializable(typeof(ExecuteAutomationRuleRequest))]
[JsonSerializable(typeof(AutomationConditionRequest))]
[JsonSerializable(typeof(AutomationTimeWindowRequest))]

[JsonSerializable(typeof(object))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
