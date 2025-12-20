using Application.DTOs.DeviceDto;
using Application.Services;

namespace WebAPI.Controllers;

public static class DeviceController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var deviceApi = routes.MapGroup("/devices");

        deviceApi.MapGet("/", GetAllDevices);
        deviceApi.MapGet("/{deviceId}", GetDeviceDetails);
        deviceApi.MapPost("/", AddDevice);
        deviceApi.MapPost("/{deviceId}/commands", SendDeviceCommand);
        deviceApi.MapPost("/{deviceId}/location", AssignLocationToDevice);
        deviceApi.MapPost("/{deviceId}/gateway", AssignGatewayToDevice);
    }

    private static async Task<IResult> GetAllDevices(IDeviceService service)
    {
        var response = await service.GetAllDevices();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDeviceDetails(IDeviceService service, Guid deviceId)
    {
        var response = await service.GetDeviceDetails(deviceId);
        return Results.Ok(response);
    }

    private static async Task<IResult> AddDevice(IDeviceService service, DeviceAddRequest request)
    {
        var response = await service.AddDevice(request);
        return Results.Created($"/devices/{response.DeviceId}", response);
    }

    private static async Task<IResult> SendDeviceCommand(IDeviceService service, Guid deviceId,
        DeviceCommandRequest deviceCommandRequest)
    {
        await service.SendDeviceCommand(deviceId, deviceCommandRequest);
        return Results.Ok();
    }

    private static async Task<IResult> AssignLocationToDevice(IDeviceService service, Guid deviceId,
        DeviceLocationAssignRequest request)
    {
        await service.AssignLocationToDevice(deviceId, request);
        return Results.NoContent();
    }

    private static async Task<IResult> AssignGatewayToDevice(IDeviceService service, Guid deviceId,
        DeviceGatewayAssignRequest request)
    {
        await service.AssignGatewayToDevice(deviceId, request);
        return Results.NoContent();
    }
}
