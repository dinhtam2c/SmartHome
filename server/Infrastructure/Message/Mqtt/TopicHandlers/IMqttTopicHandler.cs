namespace Infrastructure.Message.Mqtt.TopicHandlers;

public interface IMqttTopicHandler
{
    Task HandleAsync(MqttRouteContext routeContext, string payload, CancellationToken cancellationToken = default);
}
