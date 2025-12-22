using Application.Services;

namespace WebAPI.Controllers;

public static class DashboardController
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var dashboardApi = routes.MapGroup("/dashboard");
        dashboardApi.MapGet("/homes", GetHomeListDashboard);
        dashboardApi.MapGet("/home/{homeId}", GetHomeDashboard);
        dashboardApi.MapGet("/location/{locationId}", GetLocationDashboard);
        dashboardApi.MapGet("/device/{deviceId}", GetDeviceDashboard);
    }

    private static async Task<IResult> GetHomeListDashboard(IDashboardService service)
    {
        var response = await service.GetHomeListDashboard();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetHomeDashboard(IDashboardService service, Guid homeId)
    {
        var response = await service.GetHomeDashboard(homeId);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetLocationDashboard(IDashboardService service, Guid locationId)
    {
        var response = await service.GetLocationDashboard(locationId);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDeviceDashboard(IDashboardService service, Guid deviceId)
    {
        var response = await service.GetDeviceDashboard(deviceId);
        return Results.Ok(response);
    }
}
