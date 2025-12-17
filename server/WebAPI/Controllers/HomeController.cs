using Application.DTOs.HomeDto;
using Application.Services;
using Core.Entities;

namespace WebAPI.Controllers;

public static class HomeController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var homeApi = routes.MapGroup("/homes");

        homeApi.MapGet("/", GetHomeList);
        homeApi.MapGet("/{homeId}", GetHomeDetails);
        homeApi.MapPost("/", AddHome);
        homeApi.MapPatch("/{homeId}", UpdateHome);
        homeApi.MapDelete("/{homeId}", DeleteHome);
        homeApi.MapPost("/{homeId}/gateways", AssignGatewayToHome);
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

    private static async Task<IResult> AssignGatewayToHome(IHomeService service, Guid homeId,
        GatewayHomeAssignRequest request)
    {
        await service.AssignGatewayToHome(homeId, request);
        return Results.Ok();
    }
}
