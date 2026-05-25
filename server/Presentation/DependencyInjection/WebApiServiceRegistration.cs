using System.Text.Json.Serialization;
using Application.Ports.Realtime;
using Presentation.Middlewares;
using Presentation.Realtime.Sse;

namespace Presentation.DependencyInjection;

public static class WebApiServiceRegistration
{
    public const string CorsPolicyName = "_myAllowSpecificOrigins";

    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddCors(options =>
        {
            options.AddPolicy(
                name: CorsPolicyName,
                policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        services.AddExceptionHandler<WebApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi();
        services.AddSingleton<SseHub>();
        services.AddSingleton<IRealtimePublisher, SsePublisher>();

        return services;
    }
}
