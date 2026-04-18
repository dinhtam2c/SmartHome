using System.Text.Json.Serialization;
using WebAPI.Devices;
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

[JsonSerializable(typeof(AddSceneRequest))]
[JsonSerializable(typeof(UpdateSceneRequest))]
[JsonSerializable(typeof(ExecuteSceneRequest))]
[JsonSerializable(typeof(SceneTargetRequest))]
[JsonSerializable(typeof(SceneSideEffectRequest))]

[JsonSerializable(typeof(object))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
