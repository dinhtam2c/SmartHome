using Application.UseCases.Devices.ClaimDevice;
using Application.UseCases.Devices.AssignRoom;
using Application.UseCases.Devices.DeleteDevice;
using Application.UseCases.Devices.Control.SendCommand;
using Application.UseCases.Devices.UpdateInfo;
using Application.UseCases.Devices.GetDetails;
using MediatR;
using Presentation.Realtime.Sse;

namespace Presentation.Devices;

public static class DeviceEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var deviceApi = routes.MapGroup("/devices");

        deviceApi.MapGet("/{deviceId}", GetDeviceDetails);
        deviceApi.MapGet("/{deviceId}/events", GetDeviceRealtimeEvents);
        deviceApi.MapPost("/", AddDevice);
        deviceApi.MapPost("/{deviceId}/room", AssignRoomToDevice);
        deviceApi.MapPut("/{deviceId}", UpdateDeviceInfo);
        deviceApi.MapDelete("/{deviceId}", DeleteDevice);
        deviceApi.MapPost("/{deviceId}/commands", SendCommand);
    }

    private static async Task<IResult> GetDeviceDetails(
        Guid deviceId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetDeviceDetailsQuery(deviceId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddDevice(
        AddDeviceRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new ClaimDeviceCommand(request.HomeId, request.RoomId, request.ProvisionCode);
        var id = await sender.Send(command, ct);
        return Results.Created($"/devices/{id}", new { id });
    }

    private static async Task<IResult> UpdateDeviceInfo(
        Guid deviceId,
        UpdateDeviceInfoRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new UpdateDeviceInfoCommand(deviceId, request.Name);
        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteDevice(
        Guid deviceId,
        ISender sender,
        CancellationToken ct
    )
    {
        await sender.Send(new DeleteDeviceCommand(deviceId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AssignRoomToDevice(
        Guid deviceId,
        AssignRoomToDeviceRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new AssignRoomToDeviceCommand(deviceId, request.RoomId);
        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task<IResult> SendCommand(
        Guid deviceId,
        SendCommandRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = SendDeviceCommand.Create(
            deviceId,
            request.CapabilityId,
            request.EndpointId,
            request.Operation,
            request.Value);

        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task GetDeviceRealtimeEvents(
        Guid deviceId,
        HttpContext context,
        SseHub sseHub,
        IHostApplicationLifetime applicationLifetime)
    {
        var channel = sseHub.SubscribeToDevice(deviceId);

        await SseStreamWriter.Stream(
            context,
            channel,
            applicationLifetime.ApplicationStopping,
            () => sseHub.UnsubscribeFromDevice(deviceId, channel));
    }
}
