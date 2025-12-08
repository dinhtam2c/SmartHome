using System.Text.Json.Serialization;
using Application.DTOs.DeviceDto;
using Application.DTOs.ProvisionDto;
using Application.DTOs.SensorDataDto;

namespace Application.DTOs;

[JsonSerializable(typeof(GatewayProvisionRequest))]
[JsonSerializable(typeof(GatewayProvisionResponse))]
[JsonSerializable(typeof(DeviceProvisionRequest))]
[JsonSerializable(typeof(DeviceProvisionResponse))]
[JsonSerializable(typeof(GatewayData))]
[JsonSerializable(typeof(DeviceCommand))]
public partial class DtoJsonContext : JsonSerializerContext
{

}
