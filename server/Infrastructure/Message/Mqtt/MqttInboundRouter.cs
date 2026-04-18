using Infrastructure.Message.Mqtt.TopicHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt;

public sealed class MqttInboundRouter
{
    private sealed record HandlerRegistration(
        string TopicPattern,
        string RouteTemplate,
        Type HandlerType,
        int SpecificityScore
    );

    private static readonly IReadOnlyList<HandlerRegistration> HandlerRegistrations = new List<HandlerRegistration>
        {
            CreateHandlerRegistration<DeviceProvisionTopicHandler>(
                MqttTopics.DeviceProvision,
                "home/provision/{macAddress}/request"
            ),
            CreateHandlerRegistration<DeviceAvailabilityTopicHandler>(
                MqttTopics.DeviceAvailability,
                "home/devices/{deviceId}/availability"
            ),
            CreateHandlerRegistration<DeviceSystemStateTopicHandler>(
                MqttTopics.DeviceSystemState,
                "home/devices/{deviceId}/states/system"
            ),
            CreateHandlerRegistration<DeviceCapabilitiesStateTopicHandler>(
                MqttTopics.DeviceCapabilitiesState,
                "home/devices/{deviceId}/states/capabilities"
            ),
            CreateHandlerRegistration<DeviceCommandResultTopicHandler>(
                MqttTopics.DeviceCommandResult,
                "home/devices/{deviceId}/command/result"
            )
        }
        .OrderByDescending(h => h.SpecificityScore)
        .ThenByDescending(h => h.TopicPattern.Length)
        .ToList();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttInboundRouter> _logger;

    public IEnumerable<string> TopicPatterns => HandlerRegistrations.Select(h => h.TopicPattern);

    public MqttInboundRouter(IServiceScopeFactory scopeFactory, ILogger<MqttInboundRouter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RouteMessageAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var handlerInfo = HandlerRegistrations.FirstOrDefault(h => TopicMatch(h.TopicPattern, topic));
        if (handlerInfo is null)
        {
            _logger.LogWarning("No MQTT topic handler found for topic {topic}", topic);
            return;
        }

        var topicTokens = topic.Split('/');
        if (!TryExtractRouteValues(handlerInfo.RouteTemplate, topicTokens, out var routeValues))
        {
            _logger.LogWarning(
                "Topic {topic} does not match route template {template}",
                topic,
                handlerInfo.RouteTemplate
            );
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = (IMqttTopicHandler)scope.ServiceProvider.GetRequiredService(handlerInfo.HandlerType);
            var routeContext = new MqttRouteContext(topic, topicTokens, routeValues);
            await handler.HandleAsync(routeContext, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while processing MQTT topic {topic}", topic);
        }
    }

    private static HandlerRegistration CreateHandlerRegistration<THandler>(string topicPattern, string routeTemplate)
        where THandler : IMqttTopicHandler
    {
        return new HandlerRegistration(
            topicPattern,
            routeTemplate,
            typeof(THandler),
            CalculateSpecificityScore(topicPattern)
        );
    }

    private static int CalculateSpecificityScore(string pattern)
    {
        var score = 0;
        foreach (var segment in pattern.Split('/'))
        {
            score += segment switch
            {
                "#" => 0,
                "+" => 1,
                _ => 2
            };
        }

        return score;
    }

    private static bool TopicMatch(string pattern, string topic)
    {
        var patternParts = pattern.Split('/');
        var topicParts = topic.Split('/');

        if (topicParts.Length > patternParts.Length)
            return false;

        for (var index = 0; index < patternParts.Length; index++)
        {
            if (patternParts[index] == "#")
                return true;

            if (index >= topicParts.Length)
                return false;

            if (patternParts[index] == "+")
                continue;

            if (!string.Equals(patternParts[index], topicParts[index], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool TryExtractRouteValues(
        string routeTemplate,
        string[] topicTokens,
        out Dictionary<string, string> routeValues
    )
    {
        routeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var templateTokens = routeTemplate.Split('/');

        if (templateTokens.Length != topicTokens.Length)
            return false;

        for (var index = 0; index < templateTokens.Length; index++)
        {
            var templateToken = templateTokens[index];
            var topicToken = topicTokens[index];

            if (templateToken == "+")
                continue;

            if (IsRouteParameter(templateToken, out var parameterName))
            {
                routeValues[parameterName] = topicToken;
                continue;
            }

            if (!string.Equals(templateToken, topicToken, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool IsRouteParameter(string token, out string parameterName)
    {
        parameterName = string.Empty;

        if (token.Length < 3 || token[0] != '{' || token[^1] != '}')
            return false;

        parameterName = token[1..^1];
        return parameterName.Length != 0;
    }
}
