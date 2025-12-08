using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Message;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace Infrastructure.Mqtt;

public class MqttService : IHostedService, IMessagePublisher
{
    private readonly ManagedMqttClientOptions _options;
    private readonly IManagedMqttClient _client;

    private readonly IEnumerable<MqttTopicFilter> _topicFilters;

    private readonly MessageRouter _messageRouter;

    private readonly ILogger<MqttService> _logger;

    private readonly JsonSerializerOptions _jsonOptions;

    public MqttService(MqttOptions mqttOptions, MessageRouter messageRouter, ILogger<MqttService> logger)
    {
        _messageRouter = messageRouter;
        _logger = logger;

        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttOptions.Host, mqttOptions.Port)
            .WithClientId(mqttOptions.ClientId)
            .WithCleanStart(false)
            .WithSessionExpiryInterval(uint.MaxValue)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);

        if (!string.IsNullOrEmpty(mqttOptions.Username))
            clientOptionsBuilder.WithCredentials(mqttOptions.Username, mqttOptions.Password);

        var clientOptions = clientOptionsBuilder.Build();

        _options = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptions)
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(mqttOptions.AutoReconnectDelay))
            .Build();

        _client = new MqttFactory().CreateManagedMqttClient();
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            string topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.PayloadSegment.Array is not null
                ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                : string.Empty;

            // _logger.LogInformation("Message received from {topic}: {message}", topic, payload);
            _logger.LogInformation("Message received from {topic}", topic);
            await _messageRouter.RouteMessage(topic, payload);
        };

        _topicFilters = _messageRouter.TopicPatterns.Select(tp =>
            new MqttTopicFilterBuilder()
                .WithTopic(tp)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()
        );

        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = DtoJsonContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.SubscribeAsync(_topicFilters);
        await _client.StartAsync(_options);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    public async Task PublishMessage(string topic, object? message, MessagePolicy policy)
    {
        string? payload = null;
        if (message is not null)
        {
            payload = JsonSerializer.Serialize(message, _jsonOptions);
        }

        MqttQualityOfServiceLevel qos = policy.Qos switch
        {
            0 => MqttQualityOfServiceLevel.AtMostOnce,
            1 => MqttQualityOfServiceLevel.AtLeastOnce,
            2 => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => throw new ArgumentOutOfRangeException("policy", "Invalid Qos level")
        };

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(policy.Retained)
            .Build();

        await _client.EnqueueAsync(mqttMessage);
    }
}
