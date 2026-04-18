using Application.Commands.Devices.AddDevice;
using Application.Commands.Devices.AssignRoomToDevice;
using Application.Commands.Devices.DeleteDevice;
using Application.Commands.Devices.SendDeviceCommand;
using Application.Commands.Devices.UpdateDeviceInfo;
using Application.Queries.Devices.GetDeviceCapabilityStateHistory;
using Application.Queries.Devices.GetDeviceCommandExecutions;
using Application.Queries.Devices.GetDeviceDetails;
using Core.Domain.Devices;
using Infrastructure.Realtime.Sse;
using MediatR;
using WebAPI.Realtime;

namespace WebAPI.Devices;

public static class DeviceEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var deviceApi = routes.MapGroup("/devices");

        deviceApi.MapGet("/{deviceId}", GetDeviceDetails);
        deviceApi.MapGet("/{deviceId}/events", GetDeviceDetailsEvents);
        deviceApi.MapGet("/{deviceId}/commands/history", GetDeviceCommandHistory);
        deviceApi.MapGet("/{deviceId}/endpoints/{endpointId}/commands", GetDeviceCommandHistory);
        deviceApi.MapGet("/{deviceId}/capabilities/history", GetCapabilityHistory);
        deviceApi.MapGet(
            "/{deviceId}/endpoints/{endpointId}/capabilities/{capabilityId}/history",
            GetCapabilityHistory);
        deviceApi.MapPost("/", AddDevice);
        deviceApi.MapPost("/{deviceId}/room", AssignRoomToDevice);
        deviceApi.MapPut("/{deviceId}", UpdateDeviceInfo);
        deviceApi.MapDelete("/{deviceId}", DeleteDevice);
        deviceApi.MapPost("/{deviceId}/commands", SendDeviceCommand);
    }

    private static async Task<IResult> GetDeviceDetails(
        Guid deviceId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetDeviceDetailsQuery(deviceId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDeviceCommandHistory(
        Guid deviceId,
        ISender sender,
        CancellationToken ct,
        string? endpointId = null,
        string? capabilityId = null,
        string? correlationId = null,
        CommandLifecycleStatus? status = null,
        long? from = null,
        long? to = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetDeviceCommandExecutionsQuery(
            deviceId,
            endpointId,
            capabilityId,
            correlationId,
            status,
            from,
            to,
            page,
            pageSize);

        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCapabilityHistory(
        Guid deviceId,
        ISender sender,
        CancellationToken ct,
        string? endpointId = null,
        string? capabilityId = null,
        long? from = null,
        long? to = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = new GetDeviceCapabilityStateHistoryQuery(
            deviceId,
            endpointId,
            capabilityId,
            from,
            to,
            page,
            pageSize);

        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddDevice(
        AddDeviceRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new AddDeviceCommand(request.HomeId, request.RoomId, request.ProvisionCode);
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

    private static async Task<IResult> SendDeviceCommand(
        Guid deviceId,
        SendCommandRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new SendDeviceCommandCommand(
            deviceId,
            request.CapabilityId,
            request.EndpointId,
            request.Operation,
            request.Value,
            request.CorrelationId);

        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task GetDeviceDetailsEvents(
        Guid deviceId,
        HttpContext context,
        ISseHub sseHub)
    {
        var channel = sseHub.SubscribeToDevice(deviceId);

        await SseStreamWriter.Stream(
            context,
            channel,
            () => sseHub.UnsubscribeFromDevice(deviceId, channel));
    }
}
