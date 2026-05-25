using System.Text.Json.Serialization;
using Presentation.Automations;
using Presentation.ActionSets;
using Presentation.Devices;
using Presentation.Floors;
using Presentation.Homes;
using Presentation.Rooms;
using Presentation.Scenes;

namespace Presentation;

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
[JsonSerializable(typeof(CreateFloorPlanRoomRequest))]
[JsonSerializable(typeof(UpdateFloorPlanRoomRequest))]
[JsonSerializable(typeof(PlaceDeviceRequest))]
[JsonSerializable(typeof(MoveDeviceRequest))]

[JsonSerializable(typeof(AddSceneRequest))]
[JsonSerializable(typeof(UpdateSceneRequest))]
[JsonSerializable(typeof(ActionSetRequest))]
[JsonSerializable(typeof(ActionSetHooksRequest))]
[JsonSerializable(typeof(ActionSetExecutionPolicyRequest))]
[JsonSerializable(typeof(ActionTargetRequest))]
[JsonSerializable(typeof(ActionRequest))]

[JsonSerializable(typeof(AddAutomationRuleRequest))]
[JsonSerializable(typeof(UpdateAutomationRuleRequest))]
[JsonSerializable(typeof(AutomationConditionRequest))]
[JsonSerializable(typeof(AutomationTimeWindowRequest))]

[JsonSerializable(typeof(object))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
