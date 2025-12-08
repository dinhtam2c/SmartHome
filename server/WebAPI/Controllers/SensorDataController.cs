using Application.Services;

namespace WebAPI.Controllers;

public static class SensorDataController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var sensorDataApi = routes.MapGroup("/sensor-data");

        sensorDataApi.MapGet("/", GetAllSensorData);
    }

    private static async Task<IResult> GetAllSensorData(ISensorDataService service)
    {
        var sensorData = await service.GetAllSensorData();
        return Results.Ok(sensorData);
    }
}
