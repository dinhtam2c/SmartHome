using System.Text.Json.Serialization;
using Application.DTOs.Api.Dashboard;
using Application.DTOs.Api.Devices;
using Application.DTOs.Api.Gateways;
using Application.DTOs.Api.Homes;
using Application.DTOs.Api.Locations;

namespace WebAPI;

[JsonSerializable(typeof(IEnumerable<HomeDashboardElementDto>))]
[JsonSerializable(typeof(HomeDashboardDto))]
[JsonSerializable(typeof(LocationDashboardDto))]
[JsonSerializable(typeof(DeviceDashboardDto))]


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
