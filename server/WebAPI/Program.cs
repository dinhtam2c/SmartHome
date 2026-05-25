using WebAPI.Automations;
using WebAPI.Capabilities;
using WebAPI.DeviceCategories;
using WebAPI.DependencyInjection;
using WebAPI.Devices;
using WebAPI.Floors;
using WebAPI.Homes;
using WebAPI.Rooms;
using WebAPI.Scenes;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
    .AddWebApiServices()
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);


var app = builder.Build();

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

app.Run();
