using Application.UseCases.Floors;
using Application.UseCases.Floors.CreateFloor;
using Application.UseCases.Floors.CreateFloorPlanRoom;
using Application.UseCases.Floors.DeleteFloor;
using Application.UseCases.Floors.GetFloor;
using Application.UseCases.Floors.GetFloors;
using Application.UseCases.Floors.MoveDevice;
using Application.UseCases.Floors.PlaceDevice;
using Application.UseCases.Floors.RemoveDevicePlacement;
using Application.UseCases.Floors.RemoveFloorPlanRoom;
using Application.UseCases.Floors.ReorderFloors;
using Application.UseCases.Floors.UpdateFloorInfo;
using Application.UseCases.Floors.UpdateFloorPlanRoom;
using MediatR;

namespace Presentation.Floors;

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

        floorApi.MapPost("/{floorId}/rooms", CreateRoom);
        floorApi.MapPut("/{floorId}/rooms/{floorPlanRoomId}", UpdateRoom);
        floorApi.MapDelete("/{floorId}/rooms/{floorPlanRoomId}", RemoveRoom);

        floorApi.MapPost("/{floorId}/devices", PlaceDevice);
        floorApi.MapPatch("/{floorId}/devices/{placementId}", MoveDevice);
        floorApi.MapDelete("/{floorId}/devices/{placementId}", RemoveDevicePlacement);
    }

    private static async Task<IResult> GetFloors(Guid homeId, ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetFloorsQuery(homeId), ct));

    private static async Task<IResult> GetFloor(
        Guid homeId, Guid floorId, ISender sender, CancellationToken ct)
        => Results.Ok(await sender.Send(new GetFloorQuery(homeId, floorId), ct));

    private static async Task<IResult> CreateFloor(
        Guid homeId, CreateFloorRequest request, ISender sender, CancellationToken ct)
    {
        var floorId = await sender.Send(
            new CreateFloorCommand(homeId, request.Name, request.CanvasWidth, request.CanvasHeight), ct);
        return Results.Created($"/homes/{homeId}/floors/{floorId}", new { id = floorId });
    }

    private static async Task<IResult> ReorderFloors(
        Guid homeId, ReorderFloorsRequest request, ISender sender, CancellationToken ct)
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
        await sender.Send(new UpdateFloorInfoCommand(
            homeId, floorId, request.Name, request.CanvasWidth, request.CanvasHeight), ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteFloor(
        Guid homeId, Guid floorId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeleteFloorCommand(homeId, floorId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateRoom(
        Guid homeId,
        Guid floorId,
        CreateFloorPlanRoomRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var roomId = await sender.Send(new CreateFloorPlanRoomCommand(
            homeId,
            floorId,
            request.RoomId,
            request.Polygon?.Select(ToPointModel).ToList(),
            request.FillColor), ct);
        return Results.Created($"/homes/{homeId}/floors/{floorId}/rooms/{roomId}", new { id = roomId });
    }

    private static async Task<IResult> UpdateRoom(
        Guid homeId,
        Guid floorId,
        Guid floorPlanRoomId,
        UpdateFloorPlanRoomRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new UpdateFloorPlanRoomCommand(
            homeId,
            floorId,
            floorPlanRoomId,
            request.Polygon?.Select(ToPointModel).ToList(),
            request.FillColor), ct);
        return Results.Ok();
    }

    private static async Task<IResult> RemoveRoom(
        Guid homeId,
        Guid floorId,
        Guid floorPlanRoomId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new RemoveFloorPlanRoomCommand(homeId, floorId, floorPlanRoomId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PlaceDevice(
        Guid homeId,
        Guid floorId,
        PlaceDeviceRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var placementId = await sender.Send(new PlaceDeviceCommand(
            homeId, floorId, request.DeviceId, request.X, request.Y), ct);
        return Results.Created(
            $"/homes/{homeId}/floors/{floorId}/devices/{placementId}",
            new { id = placementId });
    }

    private static async Task<IResult> MoveDevice(
        Guid homeId,
        Guid floorId,
        Guid placementId,
        MoveDeviceRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new MoveDeviceCommand(
            homeId, floorId, placementId, request.X, request.Y), ct);
        return Results.Ok();
    }

    private static async Task<IResult> RemoveDevicePlacement(
        Guid homeId,
        Guid floorId,
        Guid placementId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new RemoveDevicePlacementCommand(
            homeId, floorId, placementId), ct);
        return Results.NoContent();
    }

    private static FloorPointModel ToPointModel(FloorPointRequest request)
        => new(request.X, request.Y);
}
