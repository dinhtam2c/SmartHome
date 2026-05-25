using Application;
using Application.Automations.Evaluation;
using Application.ActionSets;
using Application.ActionSets.Planning;
using Application.Common.Capabilities;
using Application.Common.Realtime;
using Application.Devices.CommandLifecycle;

namespace WebAPI.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
        });

        services.Configure<CommandLifecycleOptions>(
            configuration.GetSection("CommandLifecycle"));

        services.AddHostedService<CommandLifecycleTimeoutService>();
        services.AddSingleton<IAutomationEvaluationQueue, AutomationEvaluationQueue>();
        services.AddScoped<AutomationEvaluationProcessor>();
        services.AddHostedService<AutomationEvaluationBackgroundService>();

        services.AddScoped<IRealtimeDeltaNotifier, RealtimeDeltaNotifier>();
        services.AddScoped<ICapabilityCommandValidator, CapabilityCommandValidator>();
        services.AddScoped<ICapabilityProvisionValidator, CapabilityProvisionValidator>();
        services.AddScoped<ICapabilityStateValidator, CapabilityStateValidator>();
        services.AddScoped<ISetStateActionPlanner, SetStateActionPlanner>();
        services.AddScoped<IActionSetProcessor, ActionSetProcessor>();

        return services;
    }
}
