using Application.DTOs.LocationDto;
using Application.Services;

namespace WebAPI.Controllers;

public static class LocationController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var locationApi = routes.MapGroup("/locations");

        locationApi.MapGet("/", GetLocationList);
        locationApi.MapGet("/{locationId}", GetLocationDetails);
        locationApi.MapPost("/", AddLocation);
        locationApi.MapPatch("/{locationId}", UpdateLocation);
        locationApi.MapDelete("/{locationId}", DeleteLocation);
    }

    private static async Task<IResult> GetLocationList(ILocationService service, Guid? homeId = null)
    {
        var response = await service.GetLocationList(homeId);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetLocationDetails(ILocationService service, Guid locationId)
    {
        var response = await service.GetLocationDetails(locationId);
        return Results.Ok(response);
    }

    private static async Task<IResult> AddLocation(ILocationService service, LocationAddRequest request)
    {
        var response = await service.AddLocation(request);
        return Results.Created($"locations/{response.Id}", response);
    }

    private static async Task<IResult> UpdateLocation(ILocationService service, Guid locationId,
        LocationUpdateRequest request)
    {
        await service.UpdateLocation(locationId, request);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteLocation(ILocationService service, Guid locationId)
    {
        await service.DeleteLocation(locationId);
        return Results.NoContent();
    }
}
