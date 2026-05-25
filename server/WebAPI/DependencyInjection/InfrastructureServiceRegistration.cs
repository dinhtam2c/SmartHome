using Application.Common.Capabilities;
using Application.Common.Data;
using Application.Common.DeviceCategories;
using Application.Common.Message;
using Application.Common.Realtime;
using Core.Domain.Automations;
using Core.Domain.DeviceCommands;
using Core.Domain.DeviceTelemetry;
using Core.Domain.Devices;
using Core.Domain.Floors;
using Core.Domain.Homes;
using Core.Domain.Scenes;
using Infrastructure.Message.Mqtt;
using Infrastructure.Message.Mqtt.TopicHandlers;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories.Automations;
using Infrastructure.Persistence.Repositories.Devices;
using Infrastructure.Persistence.Repositories.Floors;
using Infrastructure.Persistence.Repositories.Homes;
using Infrastructure.Persistence.Repositories.Scenes;
using Infrastructure.Realtime.Sse;
using Infrastructure.Registries.Capabilities;
using Infrastructure.Registries.DeviceCategories;
using Microsoft.Extensions.Options;

namespace WebAPI.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAppReadDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IHomeRepository, HomeRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<ISceneRepository, SceneRepository>();
        services.AddScoped<ISceneExecutionRepository, SceneExecutionRepository>();
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IAutomationExecutionRepository, AutomationExecutionRepository>();
        services.AddScoped<IDeviceCommandExecutionRepository, DeviceCommandExecutionRepository>();
        services.AddScoped<IDeviceCapabilityStateHistoryRepository, DeviceCapabilityStateHistoryRepository>();

        services.Configure<MqttOptions>(configuration.GetSection("MqttOptions"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttOptions>>().Value);
        services.Configure<CapabilityRegistryFileOptions>(
            configuration.GetSection("CapabilityRegistry"));
        services.Configure<DeviceCategoryRegistryFileOptions>(
            configuration.GetSection("DeviceCategoryRegistry"));

        services.AddSingleton<ICapabilityRegistry, JsonCapabilityRegistry>();
        services.AddSingleton<IDeviceCategoryRegistry, JsonDeviceCategoryRegistry>();

        services.AddScoped<IDeviceMessagePublisher, MqttDeviceMessagePublisher>();
        services.AddSingleton<MqttInboundRouter>();
        services.AddScoped<DeviceProvisionTopicHandler>();
        services.AddScoped<DeviceAvailabilityTopicHandler>();
        services.AddScoped<DeviceSystemStateTopicHandler>();
        services.AddScoped<DeviceCapabilitiesStateTopicHandler>();
        services.AddScoped<DeviceCommandResultTopicHandler>();

        services.AddSingleton<MqttService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MqttService>());

        services.AddSingleton<ISseHub, InMemorySseHub>();
        services.AddScoped<IRealtimePublisher, SsePublisher>();

        return services;
    }
}
