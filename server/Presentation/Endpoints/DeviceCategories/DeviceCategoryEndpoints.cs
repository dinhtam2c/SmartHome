using Application.Ports.Registries;

namespace Presentation.DeviceCategories;

public static class DeviceCategoryEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var categoryApi = routes.MapGroup("/device-categories");
        categoryApi.MapGet("/", GetRegistry);
    }

    private static IResult GetRegistry(IDeviceCategoryRegistry deviceCategoryRegistry)
    {
        var response = deviceCategoryRegistry.GetAll()
            .OrderBy(definition => definition.Order)
            .ThenBy(definition => definition.DefaultName, StringComparer.OrdinalIgnoreCase)
            .Select(definition => new DeviceCategoryResponse(
                definition.Id,
                definition.DefaultName,
                definition.IconKey,
                definition.Color,
                definition.Order))
            .ToList();

        return Results.Ok(response);
    }

    private sealed record DeviceCategoryResponse(
        string Id,
        string DefaultName,
        string IconKey,
        string Color,
        int Order);
}
