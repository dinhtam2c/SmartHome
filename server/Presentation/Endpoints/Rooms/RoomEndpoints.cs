using Application.UseCases.Homes.Rooms.CreateRoom;
using Application.UseCases.Homes.Rooms.DeleteRoom;
using Application.UseCases.Homes.Rooms.UpdateRoom;
using Application.UseCases.Homes.Rooms.GetRoomDetails;
using MediatR;
using Presentation.Realtime.Sse;

namespace Presentation.Rooms;

public static class RoomEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var roomApi = routes.MapGroup("/homes/{homeId}/rooms");

        roomApi.MapGet("/{roomId}", GetRoomDetails);
        roomApi.MapGet("/{roomId}/events", GetRoomRealtimeEvents);
        roomApi.MapPost("/", AddRoom);
        roomApi.MapPatch("/{roomId}", UpdateRoom);
        roomApi.MapDelete("/{roomId}", DeleteRoom);
    }

    private static async Task<IResult> GetRoomDetails(
        Guid homeId,
        Guid roomId,
        ISender sender,
        CancellationToken ct
    )
    {
        var query = new GetRoomDetailsQuery(homeId, roomId);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddRoom(
        Guid homeId,
        AddRoomRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateRoomCommand(homeId, request.Name, request.Description);
        var id = await sender.Send(command, ct);
        return Results.Created($"/homes/{homeId}/rooms/{id}", new { id });
    }


    private static async Task<IResult> UpdateRoom(
        Guid homeId,
        Guid roomId,
        UpdateRoomRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateRoomCommand(homeId, roomId, request.Name, request.Description);
        await sender.Send(command, ct);
        return Results.Ok();
    }


    private static async Task<IResult> DeleteRoom(
        Guid homeId,
        Guid roomId,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteRoomCommand(homeId, roomId);
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task GetRoomRealtimeEvents(
        Guid homeId,
        Guid roomId,
        HttpContext context,
        SseHub sseHub,
        IHostApplicationLifetime applicationLifetime)
    {
        _ = homeId;

        var channel = sseHub.SubscribeToRoom(roomId);

        await SseStreamWriter.Stream(
            context,
            channel,
            applicationLifetime.ApplicationStopping,
            () => sseHub.UnsubscribeFromRoom(roomId, channel));
    }
}
