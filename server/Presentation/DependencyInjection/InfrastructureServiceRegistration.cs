using Application.Ports.Registries;
using Application.BusinessServices.Automations.Evaluation;
using Application.Ports.Persistence;
using Application.Ports.Messages;
using Domain.Models.Automations;
using Domain.Models.ActionSets;
using Domain.Models.Devices.Commands;
using Domain.Models.Devices;
using Domain.Models.Floors;
using Domain.Models.Homes;
using Domain.Models.Scenes;
using Infrastructure.Message.Mqtt;
using Infrastructure.BackgroundJobs;
using Infrastructure.Message.Mqtt.TopicHandlers;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories.Automations;
using Infrastructure.Persistence.Repositories.ActionSets;
using Infrastructure.Persistence.Repositories.Devices;
using Infrastructure.Persistence.Repositories.Floors;
using Infrastructure.Persistence.Repositories.Homes;
using Infrastructure.Persistence.Repositories.Scenes;
using Infrastructure.Registries;
using Microsoft.Extensions.Options;

namespace Presentation.DependencyInjection;

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
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IActionSetExecutionRepository, ActionSetExecutionRepository>();
        services.AddScoped<IDeviceCommandExecutionRepository, DeviceCommandExecutionRepository>();

        services.Configure<MqttOptions>(configuration.GetSection("MqttOptions"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttOptions>>().Value);
        services.Configure<MqttDynamicSecurityOptions>(
            configuration.GetSection("MqttDynamicSecurity"));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<MqttDynamicSecurityOptions>>().Value);
        services.Configure<CapabilityRegistryFileOptions>(
            configuration.GetSection("CapabilityRegistry"));
        services.Configure<DeviceCategoryRegistryFileOptions>(
            configuration.GetSection("DeviceCategoryRegistry"));

        services.AddSingleton<ICapabilityRegistry, JsonCapabilityRegistry>();
        services.AddSingleton<IDeviceCategoryRegistry, JsonDeviceCategoryRegistry>();

        services.AddScoped<MqttDeviceTransport>();
        services.AddScoped<IDeviceCommandSender>(sp => sp.GetRequiredService<MqttDeviceTransport>());
        services.AddScoped<IDeviceProvisioningSender>(sp => sp.GetRequiredService<MqttDeviceTransport>());

        services.Configure<CommandLifecycleOptions>(
            configuration.GetSection("CommandLifecycle"));
        services.AddHostedService<CommandLifecycleTimeoutWorker>();
        services.AddSingleton<IAutomationEvaluationQueue, AutomationEvaluationQueue>();
        services.AddHostedService<AutomationEvaluationWorker>();
        services.AddSingleton<MqttInboundRouter>();
        services.AddScoped<DeviceProvisionTopicHandler>();
        services.AddScoped<DeviceAvailabilityTopicHandler>();
        services.AddScoped<DeviceSystemStateTopicHandler>();
        services.AddScoped<DeviceCapabilitiesStateTopicHandler>();
        services.AddScoped<DeviceCommandResultTopicHandler>();

        services.AddSingleton<MqttDynamicSecurityService>();
        services.AddSingleton<IDeviceAccessManager>(sp =>
            sp.GetRequiredService<MqttDynamicSecurityService>());
        services.AddSingleton<IHostedService>(sp =>
            sp.GetRequiredService<MqttDynamicSecurityService>());
        services.AddSingleton<MqttService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MqttService>());

        return services;
    }
}
