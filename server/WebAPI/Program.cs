using System.Text.Json.Serialization;
using Application.Common.Message;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Services.MessageHandlers;
using Infrastructure.Mqtt;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using WebAPI;
using WebAPI.Controllers;
using WebAPI.Middlewares;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});

builder.Services.AddExceptionHandler<WebApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();


builder.Services.AddSingleton<MessageRouter>();
builder.Services.AddSingleton<IMessageHandler, GatewayProvisionHandler>();
builder.Services.AddSingleton<GatewayProvisionHandler>();
builder.Services.AddSingleton<IMessageHandler, DeviceProvisionHandler>();
builder.Services.AddSingleton<DeviceProvisionHandler>();
builder.Services.AddSingleton<IMessageHandler, GatewayAvailabilityHandler>();
builder.Services.AddSingleton<GatewayAvailabilityHandler>();
builder.Services.AddSingleton<IMessageHandler, DeviceAvailabilityHandler>();
builder.Services.AddSingleton<DeviceAvailabilityHandler>();
builder.Services.AddSingleton<IMessageHandler, DeviceActuatorsStatesHandler>();
builder.Services.AddSingleton<DeviceActuatorsStatesHandler>();
builder.Services.AddSingleton<IMessageHandler, GatewayDataHandler>();
builder.Services.AddSingleton<GatewayDataHandler>();


builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IHomeService, HomeService>();

builder.Services.AddScoped<IGatewayRepository, GatewayRepository>();
builder.Services.AddScoped<IGatewayService, GatewayService>();

builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ILocationService, LocationService>();

builder.Services.AddScoped<ISensorRepository, SensorRepository>();

builder.Services.AddScoped<ISensorDataRepository, SensorDataRepository>();
builder.Services.AddScoped<ISensorDataService, SensorDataService>();


builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("MqttOptions"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttOptions>>().Value);
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<MqttService>());


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

app.UseCors(MyAllowSpecificOrigins);

var api = app.MapGroup("/api/v1");

HomeController.MapEndpoints(api);
GatewayController.MapEndpoints(api);
DeviceController.MapEndpoints(api);
LocationController.MapEndpoints(api);

app.Run();
