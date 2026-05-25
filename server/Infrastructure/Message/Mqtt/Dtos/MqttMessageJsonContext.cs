using System.Text.Json.Serialization;

namespace Infrastructure.Message.Mqtt.Dtos;

[JsonSerializable(typeof(ProvisionDeviceMessage))]
[JsonSerializable(typeof(DeviceAvailabilityMessage))]
[JsonSerializable(typeof(DeviceSystemStateMessage))]
[JsonSerializable(typeof(IEnumerable<DeviceCapabilityStateMessage>))]
[JsonSerializable(typeof(DeviceCommandResultMessage))]

[JsonSerializable(typeof(DeviceProvisionResponseMessage))]
[JsonSerializable(typeof(DeviceCredentialsResponseMessage))]
[JsonSerializable(typeof(DeviceCommandMessage))]
public partial class MqttMessageJsonContext : JsonSerializerContext
{

}
