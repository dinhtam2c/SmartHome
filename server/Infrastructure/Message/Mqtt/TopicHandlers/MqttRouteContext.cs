namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed record MqttRouteContext(
    string Topic,
    string[] TopicTokens,
    IReadOnlyDictionary<string, string> RouteValues
)
{
    public string GetRequired(string key)
    {
        if (RouteValues.TryGetValue(key, out var value))
            return value;

        throw new KeyNotFoundException($"Route value '{key}' not found for topic '{Topic}'");
    }
}
