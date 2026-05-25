using Application.UseCases.Homes.CreateHome;
using Application.UseCases.Homes.DeleteHome;
using Application.UseCases.Homes.UpdateHome;
using Application.UseCases.Homes.GetHomeDetails;
using Application.UseCases.Homes.GetHomeDevices;
using Application.UseCases.Homes.GetHomes;
using MediatR;
using Presentation.Realtime.Sse;

namespace Presentation.Homes;

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

        homeApi.MapGet("/{homeId}/events", GetHomeRealtimeEvents);
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
        var command = new CreateHomeCommand(request.Name, request.Description);
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

    private static async Task GetHomeRealtimeEvents(
        Guid homeId,
        HttpContext context,
        SseHub sseHub,
        IHostApplicationLifetime applicationLifetime)
    {
        var channel = sseHub.SubscribeToHome(homeId);

        await SseStreamWriter.Stream(
            context,
            channel,
            applicationLifetime.ApplicationStopping,
            () => sseHub.UnsubscribeFromHome(homeId, channel));
    }
}
