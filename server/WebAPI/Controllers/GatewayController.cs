using Application.Services;

namespace WebAPI.Controllers;

public static class GatewayController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var gatewayApi = routes.MapGroup("/gateways");

        gatewayApi.MapGet("/", GetAllGateways);
    }

    private static async Task<IResult> GetAllGateways(IGatewayService service)
    {
        var response = await service.GetAllGateways();
        return Results.Ok(response);
    }
}
