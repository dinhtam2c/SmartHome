using System.Text.Json.Serialization;
using Application.DTOs.DeviceDto;
using Application.DTOs.GatewayDto;
using Application.DTOs.HomeDto;
using Application.DTOs.SensorDataDto;

namespace WebAPI;

[JsonSerializable(typeof(IEnumerable<HomeListElement>))]
[JsonSerializable(typeof(HomeDetails))]
[JsonSerializable(typeof(HomeAddRequest))]
[JsonSerializable(typeof(HomeAddResponse))]
[JsonSerializable(typeof(HomeUpdateRequest))]


[JsonSerializable(typeof(IEnumerable<GatewayListElement>))]


[JsonSerializable(typeof(IEnumerable<DeviceListElement>))]
[JsonSerializable(typeof(DeviceDetails))]
[JsonSerializable(typeof(DeviceAddRequest))]
[JsonSerializable(typeof(DeviceAddResponse))]
[JsonSerializable(typeof(DeviceCommandRequest))]


[JsonSerializable(typeof(IEnumerable<SensorDataResponse>))]

internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
