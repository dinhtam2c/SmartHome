using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace Infrastructure.Message.Mqtt;

public sealed class MqttService : IHostedService, IDisposable
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(2);

    private readonly ManagedMqttClientOptions _options;
    private readonly IManagedMqttClient _client;

    private readonly IEnumerable<MqttTopicFilter> _topicFilters;

    private readonly MqttInboundRouter _inboundRouter;

    private readonly ILogger<MqttService> _logger;

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CancellationToken _applicationStopping;
    private bool _disposed;

    public MqttService(
        MqttOptions mqttOptions,
        MqttInboundRouter inboundRouter,
        ILogger<MqttService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _inboundRouter = inboundRouter;
        _logger = logger;
        _applicationStopping = applicationLifetime.ApplicationStopping;

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
        _client.ApplicationMessageReceivedAsync += HandleApplicationMessage;

        _topicFilters = _inboundRouter.TopicPatterns.Select(tp =>
            new MqttTopicFilterBuilder()
                .WithTopic(tp)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()
        );

        _jsonOptions = MqttJsonSerializerOptions.Create();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.SubscribeAsync(_topicFilters);
        await _client.StartAsync(_options);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _client.ApplicationMessageReceivedAsync -= HandleApplicationMessage;

        var stopTask = _client.StopAsync();
        var timeoutTask = Task.Delay(StopTimeout);

        if (await Task.WhenAny(stopTask, timeoutTask) == stopTask)
        {
            await stopTask;
            return;
        }

        _logger.LogWarning(
            "MQTT client did not stop within {TimeoutSeconds} seconds; disposing it to complete host shutdown.",
            StopTimeout.TotalSeconds);
        _ = stopTask.ContinueWith(
            task => _logger.LogDebug(task.Exception, "MQTT client stop completed after forced dispose."),
            TaskContinuationOptions.OnlyOnFaulted);

        DisposeClient();
    }

    public void Dispose()
    {
        DisposeClient();
    }

    public async Task PublishMessage(string topic, object? message, int qos, bool retained)
    {
        string? payload = null;
        if (message is not null)
        {
            payload = JsonSerializer.Serialize(message, _jsonOptions);
        }

        MqttQualityOfServiceLevel qosLevel = qos switch
        {
            0 => MqttQualityOfServiceLevel.AtMostOnce,
            1 => MqttQualityOfServiceLevel.AtLeastOnce,
            2 => MqttQualityOfServiceLevel.ExactlyOnce,
            _ => throw new ArgumentOutOfRangeException("policy", "Invalid Qos level")
        };

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qosLevel)
            .WithRetainFlag(retained)
            .Build();

        await _client.EnqueueAsync(mqttMessage);
    }

    private async Task HandleApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic;
            var payload = eventArgs.ApplicationMessage.PayloadSegment.Array is not null
                ? Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment)
                : string.Empty;

            await _inboundRouter.RouteMessageAsync(topic, payload, _applicationStopping);
        }
        catch (OperationCanceledException) when (_applicationStopping.IsCancellationRequested)
        {
        }
    }

    private void DisposeClient()
    {
        if (_disposed)
            return;

        _disposed = true;
        _client.ApplicationMessageReceivedAsync -= HandleApplicationMessage;

        if (_client is IDisposable disposableClient)
            disposableClient.Dispose();
    }
}
