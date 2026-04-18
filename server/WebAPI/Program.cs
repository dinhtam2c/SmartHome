using System.Text.Json.Serialization;
using Application;
using Application.Commands.Scenes.ExecuteScene;
using Application.Common.Data;
using Application.Common.Message;
using Application.Common.Realtime;
using Application.Services;
using Core.Domain.Data;
using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Core.Domain.Homes;
using Core.Domain.Scenes;
using Infrastructure.Message.Mqtt;
using Infrastructure.Message.Mqtt.TopicHandlers;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Realtime.Sse;
using Microsoft.Extensions.Options;
using WebAPI;
using WebAPI.Capabilities;
using WebAPI.Devices;
using WebAPI.Homes;
using WebAPI.Rooms;
using WebAPI.Middlewares;
using WebAPI.Scenes;

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

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
});

// Persistence
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IAppReadDbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISceneRepository, SceneRepository>();
builder.Services.AddScoped<ISceneExecutionRepository, SceneExecutionRepository>();
builder.Services.AddScoped<IDeviceCommandExecutionRepository, DeviceCommandExecutionRepository>();
builder.Services.AddScoped<IDeviceCapabilityStateHistoryRepository, DeviceCapabilityStateHistoryRepository>();

// Device mqtt message publisher
builder.Services.AddScoped<IDeviceMessagePublisher, MqttDeviceMessagePublisher>();

// Device mqtt message handler
builder.Services.AddSingleton<MqttInboundRouter>();
builder.Services.AddScoped<DeviceProvisionTopicHandler>();
builder.Services.AddScoped<DeviceAvailabilityTopicHandler>();
builder.Services.AddScoped<DeviceSystemStateTopicHandler>();
builder.Services.AddScoped<DeviceCapabilitiesStateTopicHandler>();
builder.Services.AddScoped<DeviceCommandResultTopicHandler>();

// Realtime communication
builder.Services.AddSingleton<ISseHub, InMemorySseHub>();
builder.Services.AddScoped<IRealtimePublisher, SsePublisher>();
builder.Services.AddScoped<IRealtimeDetailsNotifier, RealtimeDetailsNotifier>();

// Hosted services
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService<CommandLifecycleTimeoutService>();

// Configurations
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("MqttOptions"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttOptions>>().Value);
builder.Services.Configure<CommandLifecycleOptions>(builder.Configuration.GetSection("CommandLifecycle"));
builder.Services.Configure<CapabilityRegistryFileOptions>(builder.Configuration.GetSection("CapabilityRegistry"));



builder.Services.AddScoped<ICapabilityCommandValidator, CapabilityCommandValidator>();
builder.Services.AddSingleton<ICapabilityRegistry, JsonCapabilityRegistry>();
builder.Services.AddScoped<ICapabilityRegistryValidator, CapabilityRegistryValidator>();
builder.Services.AddScoped<ICapabilityStateValidator, CapabilityStateValidator>();
builder.Services.AddScoped<IScenePlanner, ScenePlanner>();




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

CapabilityRegistryEndpoints.MapEndpoints(api);
HomeEndpoints.MapEndpoints(api);
RoomEndpoints.MapEndpoints(api);
DeviceEndpoints.MapEndpoints(api);
SceneEndpoints.MapEndpoints(api);

app.Run();
