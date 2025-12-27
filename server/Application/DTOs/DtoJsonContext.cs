using System.Text.Json.Serialization;
using Application.DTOs.DeviceDto;
using Application.DTOs.GatewayDto;
using Application.DTOs.ProvisionDto;
using Application.DTOs.SensorDataDto;

namespace Application.DTOs;

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
public partial class DtoJsonContext : JsonSerializerContext
{

}
