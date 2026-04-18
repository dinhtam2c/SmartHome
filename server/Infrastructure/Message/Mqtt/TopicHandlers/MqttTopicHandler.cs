using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public abstract class MqttTopicHandler<TMessage> : IMqttTopicHandler
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = MqttMessageJsonContext.Default
    };

    protected ILogger Logger { get; }

    protected MqttTopicHandler(ILogger logger)
    {
        Logger = logger;
    }

    public async Task HandleAsync(
        MqttRouteContext routeContext,
        string payload,
        CancellationToken cancellationToken = default
    )
    {
        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(payload, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse MQTT payload for topic {topic}", routeContext.Topic);
            return;
        }

        if (message is null)
        {
            Logger.LogWarning("MQTT payload parsed to null for topic {topic}", routeContext.Topic);
            return;
        }

        await HandleMessageAsync(routeContext, message, cancellationToken);
    }

    protected abstract Task HandleMessageAsync(
        MqttRouteContext routeContext,
        TMessage message,
        CancellationToken cancellationToken
    );
}

public abstract class MqttMediatRTopicHandler<TMessage> : MqttTopicHandler<TMessage>
{
    private readonly ISender _sender;

    protected MqttMediatRTopicHandler(
        ISender sender,
        ILogger logger
    )
        : base(logger)
    {
        _sender = sender;
    }

    protected sealed override Task HandleMessageAsync(
        MqttRouteContext routeContext,
        TMessage message,
        CancellationToken cancellationToken
    )
    {
        var request = MapToRequest(routeContext, message);
        return _sender.Send(request, cancellationToken);
    }

    protected abstract object MapToRequest(
        MqttRouteContext routeContext,
        TMessage message
    );
}
