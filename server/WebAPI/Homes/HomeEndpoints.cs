using Application.Commands.Homes.AddHome;
using Application.Commands.Homes.DeleteHome;
using Application.Commands.Homes.UpdateHome;
using Application.Queries.Homes.GetHomeDetails;
using Application.Queries.Homes.GetHomeDevices;
using Application.Queries.Homes.GetHomes;
using Infrastructure.Realtime.Sse;
using MediatR;
using WebAPI.Realtime;

namespace WebAPI.Homes;

public static class HomeEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var homeApi = routes.MapGroup("/homes");

        homeApi.MapGet("/", GetHomes);
        homeApi.MapGet("/{homeId}", GetHomeDetails);
        homeApi.MapGet("/{homeId}/devices", GetHomeDevices);
        homeApi.MapPost("/", AddHome);
        homeApi.MapPatch("/{homeId}", UpdateHome);
        homeApi.MapDelete("/{homeId}", DeleteHome);

        homeApi.MapGet("/{homeId}/events", GetHomeDetailsEvents);
    }

    private static async Task<IResult> GetHomes(ISender sender, CancellationToken ct)
    {
        var query = new GetHomesQuery();
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHomeDetails(Guid homeId, ISender sender, CancellationToken ct)
    {
        var query = new GetHomeDetailsQuery(homeId);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHomeDevices(
        Guid homeId,
        ISender sender,
        CancellationToken ct,
        Guid? roomId = null)
    {
        var query = new GetHomeDevicesQuery(homeId, roomId);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddHome(
        AddHomeRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new AddHomeCommand(request.Name, request.Description);
        var id = await sender.Send(command, ct);
        return Results.Created($"/homes/{id}", new { id });
    }

    private static async Task<IResult> UpdateHome(
        Guid homeId,
        UpdateHomeRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateHomeCommand(homeId, request.Name, request.Description);
        await sender.Send(command, ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteHome(
        Guid homeId,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteHomeCommand(homeId);
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task GetHomeDetailsEvents(
        Guid homeId,
        HttpContext context,
        ISseHub sseHub)
    {
        var channel = sseHub.SubscribeToHome(homeId);

        await SseStreamWriter.Stream(
            context,
            channel,
            () => sseHub.UnsubscribeFromHome(homeId, channel));
    }
}
