using Application;
using Application.BusinessServices.ActionSets.Execution;
using Application.BusinessServices.ActionSets.Planning;
using Application.BusinessServices.Automations.Evaluation;
using Application.BusinessServices.Capabilities.Validation;
using Application.BusinessServices.Devices.ControlLifecycle;
using Application.BusinessServices.Devices.Realtime;
using Application.BusinessServices.Devices.State;
using Application.BusinessServices.Floors;
using Application.BusinessServices.Homes.RoomsRealtime;

namespace Presentation.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
        });

        services.AddScoped<AutomationEvaluationProcessor>();
        services.AddScoped<CommandTimeoutProcessor>();

        services.AddScoped<IDeviceRealtimeNotifier, DeviceRealtimeNotifier>();
        services.AddScoped<IRoomRealtimeNotifier, RoomRealtimeNotifier>();
        services.AddSingleton<IJsonSchemaPayloadEvaluator, JsonSchemaPayloadEvaluator>();
        services.AddScoped<ICapabilityCommandValidator, CapabilityCommandValidator>();
        services.AddScoped<ICapabilityProvisionValidator, CapabilityProvisionValidator>();
        services.AddScoped<ICapabilityStateValidator, CapabilityStateValidator>();
        services.AddScoped<ICapabilityStateUpdater, CapabilityStateUpdater>();
        services.AddScoped<ISetStateActionPlanner, SetStateActionPlanner>();
        services.AddScoped<IActionDispatcher, ActionDispatcher>();
        services.AddScoped<IActionSetProcessor, ActionSetProcessor>();
        services.AddScoped<FloorPlanConsistencyService>();

        return services;
    }
}
