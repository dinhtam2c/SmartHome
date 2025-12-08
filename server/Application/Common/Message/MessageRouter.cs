using System.Text.Json;
using System.Text.Json.Serialization;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Common.Message;

public class MessageRouter
{
    private record HandlerInfo(Type MessageType, Type HandlerType);

    private readonly Dictionary<string, HandlerInfo> _handlers;

    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<MessageRouter> _logger;

    public IEnumerable<string> TopicPatterns => _handlers.Keys;

    public MessageRouter(IServiceScopeFactory scopeFactory, IEnumerable<IMessageHandler> handlers,
        ILogger<MessageRouter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _handlers = handlers.ToDictionary(
            h => h.TopicPattern,
            h => new HandlerInfo(h.MessageType, h.GetType())
        );

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = DtoJsonContext.Default
        };
    }

    public async Task RouteMessage(string topic, string payload)
    {
        var handlerInfo = _handlers.FirstOrDefault(h => TopicMatch(h.Key, topic)).Value;

        if (handlerInfo is null)
        {
            _logger.LogWarning("No handlerInfo found for topic {topic}", topic);
            return;
        }

        object? message = null;
        try
        {
            message = JsonSerializer.Deserialize(payload, handlerInfo.MessageType, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Json parse error");
        }

        if (message is null)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerInfo.HandlerType);
            await handler.HandleMessage(topic, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while processing message");
        }
    }

    private bool TopicMatch(string pattern, string topic)
    {
        var patternParts = pattern.Split('/');
        var topicParts = topic.Split('/');

        if (topicParts.Length > patternParts.Length)
            return false;

        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "#")
                return true;

            if (i >= topicParts.Length)
                return false;

            if (patternParts[i] == "+")
                continue;

            if (patternParts[i] != topicParts[i])
                return false;
        }

        return true;
    }
}
