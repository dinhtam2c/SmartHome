using System.Text.Json.Serialization;
using Application.DTOs.Messages.Devices;
using Application.DTOs.Messages.Gateways;
using Application.DTOs.Messages.Provision;

namespace Application.DTOs.Messages;

[JsonSerializable(typeof(GatewayProvisionRequest))]
[JsonSerializable(typeof(GatewayProvisionResponse))]
[JsonSerializable(typeof(GatewayAvailability))]
[JsonSerializable(typeof(GatewayState))]

[JsonSerializable(typeof(DeviceProvisionRequest))]
[JsonSerializable(typeof(DeviceProvisionResponse))]
[JsonSerializable(typeof(DeviceAvailability))]
[JsonSerializable(typeof(DeviceSystemState))]
[JsonSerializable(typeof(IEnumerable<DeviceActuatorStates>))]
[JsonSerializable(typeof(DeviceCommand))]

[JsonSerializable(typeof(GatewayData))]
public partial class MessageDtoJsonContext : JsonSerializerContext
{

}
