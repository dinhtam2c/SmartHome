using Application.DTOs.HomeDto;
using Application.Services;

namespace WebAPI.Controllers;

public static class HomeController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var gatewayApi = routes.MapGroup("/homes");

        gatewayApi.MapGet("/", GetHomeList);
        gatewayApi.MapGet("/{homeId}", GetHomeDetails);
        gatewayApi.MapPost("/", AddHome);
        gatewayApi.MapPatch("/{homeId}", UpdateHome);
        gatewayApi.MapDelete("/{homeId}", DeleteHome);
    }

    private static async Task<IResult> GetHomeList(IHomeService service)
    {
        var response = await service.GetHomeList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetHomeDetails(IHomeService service, Guid homeId)
    {
        var response = await service.GetHomeDetails(homeId);
        return Results.Ok(response);
    }

    private static async Task<IResult> AddHome(IHomeService service, HomeAddRequest request)
    {
        var response = await service.AddHome(request);
        return Results.Created($"homes/{response.Id}", response);
    }

    private static async Task<IResult> UpdateHome(IHomeService service, Guid homeId, HomeUpdateRequest request)
    {
        await service.UpdateHome(homeId, request);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteHome(IHomeService service, Guid homeId)
    {
        await service.DeleteHome(homeId);
        return Results.NoContent();
    }
}
