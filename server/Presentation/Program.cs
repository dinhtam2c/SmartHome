using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Presentation.Automations;
using Presentation.Capabilities;
using Presentation.DeviceCategories;
using Presentation.DependencyInjection;
using Presentation.Devices;
using Presentation.Floors;
using Presentation.Homes;
using Presentation.Rooms;
using Presentation.Scenes;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddWebApiServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);


var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Open API v1");
    });
}

app.UseCors(WebApiServiceRegistration.CorsPolicyName);

var api = app.MapGroup("/api/v1");

CapabilityRegistryEndpoints.MapEndpoints(api);
DeviceCategoryEndpoints.MapEndpoints(api);
HomeEndpoints.MapEndpoints(api);
RoomEndpoints.MapEndpoints(api);
DeviceEndpoints.MapEndpoints(api);
FloorEndpoints.MapEndpoints(api);
SceneEndpoints.MapEndpoints(api);
AutomationEndpoints.MapEndpoints(api);

await app.RunAsync();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    try
    {
        logger.LogInformation("Applying database migrations.");

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(app.Lifetime.ApplicationStopping);

        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "Database migration failed; application startup aborted.");
        throw;
    }
}
