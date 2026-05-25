using Application.Commands.Floors;
using Application.Commands.Floors.CreateFloor;
using Application.Commands.Floors.DeleteFloor;
using Application.Commands.Floors.MoveDevice;
using Application.Commands.Floors.PlaceDevice;
using Application.Commands.Floors.RemoveFloorRoom;
using Application.Commands.Floors.RemovePlacedFloorDevice;
using Application.Commands.Floors.ReorderFloors;
using Application.Commands.Floors.UpdateFloorInfo;
using Application.Commands.Floors.UpsertFloorRoom;
using Application.Queries.Floors.GetFloor;
using Application.Queries.Floors.GetFloors;
using MediatR;

namespace WebAPI.Floors;

public static class FloorEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var floorApi = routes.MapGroup("/homes/{homeId}/floors");

        floorApi.MapGet("/", GetFloors);
        floorApi.MapPost("/", CreateFloor);
        floorApi.MapPut("/order", ReorderFloors);

        floorApi.MapGet("/{floorId}", GetFloor);
        floorApi.MapPatch("/{floorId}", UpdateFloorInfo);
        floorApi.MapDelete("/{floorId}", DeleteFloor);

        floorApi.MapPost("/{floorId}/rooms", UpsertRoom);
        floorApi.MapPut("/{floorId}/rooms/{roomId}", UpsertRoom);
        floorApi.MapDelete("/{floorId}/rooms/{roomId}", RemoveRoom);

        floorApi.MapPost("/{floorId}/devices", PlaceDevice);
        floorApi.MapPatch("/{floorId}/devices/{placedFloorDeviceId}", MoveDevice);
        floorApi.MapDelete("/{floorId}/devices/{placedFloorDeviceId}", RemovePlacedFloorDevice);
    }

    private static async Task<IResult> GetFloors(
        Guid homeId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetFloorsQuery(homeId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetFloor(
        Guid homeId,
        Guid floorId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetFloorQuery(homeId, floorId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateFloor(
        Guid homeId,
        CreateFloorRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var floorId = await sender.Send(
            new CreateFloorCommand(
                homeId,
                request.Name,
                request.CanvasWidth,
                request.CanvasHeight),
            ct);

        return Results.Created($"/homes/{homeId}/floors/{floorId}", new { id = floorId });
    }

    private static async Task<IResult> ReorderFloors(
        Guid homeId,
        ReorderFloorsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new ReorderFloorsCommand(homeId, request.FloorIds), ct);
        return Results.Ok();
    }

    private static async Task<IResult> UpdateFloorInfo(
        Guid homeId,
        Guid floorId,
        UpdateFloorInfoRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new UpdateFloorInfoCommand(
                homeId,
                floorId,
                request.Name,
                request.CanvasWidth,
                request.CanvasHeight),
            ct);

        return Results.Ok();
    }

    private static async Task<IResult> DeleteFloor(
        Guid homeId,
        Guid floorId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteFloorCommand(homeId, floorId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpsertRoom(
        Guid homeId,
        Guid floorId,
        UpsertFloorRoomRequest request,
        ISender sender,
        CancellationToken ct,
        Guid? roomId = null)
    {
        var savedRoomId = await sender.Send(
            new UpsertFloorRoomCommand(
                homeId,
                floorId,
                roomId,
                request.LinkedRoomId,
                request.Label,
                request.Polygon?.Select(ToPointModel).ToList(),
                request.FillColor),
            ct);

        return roomId.HasValue
            ? Results.Ok(new { id = savedRoomId })
            : Results.Created($"/homes/{homeId}/floors/{floorId}/rooms/{savedRoomId}", new { id = savedRoomId });
    }

    private static async Task<IResult> RemoveRoom(
        Guid homeId,
        Guid floorId,
        Guid roomId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new RemoveFloorRoomCommand(homeId, floorId, roomId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PlaceDevice(
        Guid homeId,
        Guid floorId,
        PlaceDeviceRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var placedFloorDeviceId = await sender.Send(
            new PlaceDeviceCommand(
                homeId,
                floorId,
                request.DeviceId,
                request.X,
                request.Y,
                request.FloorRoomId),
            ct);

        return Results.Created(
            $"/homes/{homeId}/floors/{floorId}/devices/{placedFloorDeviceId}",
            new { id = placedFloorDeviceId });
    }

    private static async Task<IResult> MoveDevice(
        Guid homeId,
        Guid floorId,
        Guid placedFloorDeviceId,
        MoveDeviceRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new MoveDeviceCommand(
                homeId,
                floorId,
                placedFloorDeviceId,
                request.X,
                request.Y,
                request.FloorRoomId),
            ct);

        return Results.Ok();
    }

    private static async Task<IResult> RemovePlacedFloorDevice(
        Guid homeId,
        Guid floorId,
        Guid placedFloorDeviceId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new RemovePlacedFloorDeviceCommand(homeId, floorId, placedFloorDeviceId), ct);
        return Results.NoContent();
    }

    private static FloorPointModel ToPointModel(FloorPointRequest request)
    {
        return new FloorPointModel(request.X, request.Y);
    }
}
