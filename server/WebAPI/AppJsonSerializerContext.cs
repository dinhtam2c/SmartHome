using System.Text.Json.Serialization;
using Application.DTOs.DeviceDto;
using Application.DTOs.GatewayDto;
using Application.DTOs.HomeDto;
using Application.DTOs.LocationDto;

namespace WebAPI;

[JsonSerializable(typeof(IEnumerable<HomeListElement>))]
[JsonSerializable(typeof(HomeDetails))]
[JsonSerializable(typeof(HomeAddRequest))]
[JsonSerializable(typeof(HomeAddResponse))]
[JsonSerializable(typeof(HomeUpdateRequest))]
[JsonSerializable(typeof(GatewayHomeAssignRequest))]


[JsonSerializable(typeof(IEnumerable<LocationListElement>))]
[JsonSerializable(typeof(LocationDetails))]
[JsonSerializable(typeof(LocationAddRequest))]
[JsonSerializable(typeof(LocationAddResponse))]
[JsonSerializable(typeof(LocationUpdateRequest))]
[JsonSerializable(typeof(DeviceLocationAssignRequest))]


[JsonSerializable(typeof(IEnumerable<GatewayListElement>))]
[JsonSerializable(typeof(DeviceGatewayAssignRequest))]


[JsonSerializable(typeof(IEnumerable<DeviceListElement>))]
[JsonSerializable(typeof(DeviceDetails))]
[JsonSerializable(typeof(DeviceAddRequest))]
[JsonSerializable(typeof(DeviceAddResponse))]
[JsonSerializable(typeof(DeviceCommandRequest))]

internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
