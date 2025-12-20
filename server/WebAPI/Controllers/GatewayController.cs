using Application.DTOs.GatewayDto;
using Application.Services;

namespace WebAPI.Controllers;

public static class GatewayController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var gatewayApi = routes.MapGroup("/gateways");

        gatewayApi.MapGet("/", GetAllGateways);
        gatewayApi.MapPost("/{gatewayId}/home", AssignHomeToGateway);
    }

    private static async Task<IResult> GetAllGateways(IGatewayService service)
    {
        var response = await service.GetAllGateways();
        return Results.Ok(response);
    }

    private static async Task<IResult> AssignHomeToGateway(IGatewayService service, Guid gatewayId,
        GatewayHomeAssignRequest request)
    {
        await service.AssignHomeToGateway(gatewayId, request);
        return Results.NoContent();
    }
}
