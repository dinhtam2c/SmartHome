using Application.Commands.Homes.AddRoom;
using Application.Commands.Homes.DeleteRoom;
using Application.Commands.Homes.UpdateRoom;
using Application.Queries.Rooms.GetRoomDetails;
using Infrastructure.Realtime.Sse;
using MediatR;
using WebAPI.Realtime;

namespace WebAPI.Rooms;

public static class RoomEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var roomApi = routes.MapGroup("/homes/{homeId}/rooms");

        roomApi.MapGet("/{roomId}", GetRoomDetails);
        roomApi.MapGet("/{roomId}/events", GetRoomDetailsEvents);
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
        var command = new AddRoomCommand(homeId, request.Name, request.Description);
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

    private static async Task GetRoomDetailsEvents(
        Guid homeId,
        Guid roomId,
        HttpContext context,
        ISseHub sseHub)
    {
        _ = homeId;

        var channel = sseHub.SubscribeToRoom(roomId);

        await SseStreamWriter.Stream(
            context,
            channel,
            () => sseHub.UnsubscribeFromRoom(roomId, channel));
    }
}
