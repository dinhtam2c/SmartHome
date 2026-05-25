using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Ports.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace Infrastructure.Message.Mqtt;

public sealed class MqttDynamicSecurityService
    : IDeviceAccessManager, IHostedService, IDisposable
{
    private const string CommandTopic = "$CONTROL/dynamic-security/v1";
    private const string ResponseTopic = "$CONTROL/dynamic-security/v1/response";
    private const string BackendRole = "smarthome-backend";
    private const string DeviceRole = "smarthome-device";
    private const string ProvisioningRole = "smarthome-provisioning";
    private const string ProvisioningGroup = "smarthome-provisioning";

    private readonly MqttDynamicSecurityOptions _options;
    private readonly ILogger<MqttDynamicSecurityService> _logger;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string?>> _pendingCommands = new();
    private bool _disposed;

    public MqttDynamicSecurityService(
        MqttDynamicSecurityOptions options,
        ILogger<MqttDynamicSecurityService> logger)
    {
        _options = options;
        _logger = logger;
        _client = new MqttFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += HandleApplicationMessage;

        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .WithClientId(options.ClientId)
            .WithCredentials(options.Username, options.Password)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Mosquitto Dynamic Security integration is disabled.");
            return;
        }

        ValidateOptions();
        await EnsureConnectedAsync(cancellationToken);
        await BootstrapAuthorizationAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_client.IsConnected)
            return;

        await _client.DisconnectAsync(cancellationToken: cancellationToken);
    }

    public async Task UpsertDeviceAccess(
        Guid deviceId,
        string password,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        var username = deviceId.ToString();
        var created = await UpsertClientAsync(
            username,
            password,
            username,
            roles: Array.Empty<string>(),
            groups: new[] { _options.DeviceGroup },
            cancellationToken);

        _logger.LogInformation(
            "{Action} Mosquitto credentials for device {DeviceId}.",
            created ? "Created" : "Updated",
            deviceId);
    }

    public async Task DeleteDeviceAccess(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return;

        var username = deviceId.ToString();
        var command = new JsonObject
        {
            ["command"] = "deleteClient",
            ["username"] = username
        };

        var error = await ExecuteCommandAsync(command, cancellationToken);
        if (error is null || IsNotFoundError(error))
        {
            _logger.LogInformation("Deleted Mosquitto credentials for device {DeviceId}.", deviceId);
            return;
        }

        ThrowIfCommandFailed("deleteClient", username, error);
    }

    private async Task BootstrapAuthorizationAsync(CancellationToken cancellationToken)
    {
        await EnsureRoleAsync(BackendRole, cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientSend", "home/provision/+/response", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientSend", "home/devices/+/command", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientReceive", "home/provision/+/request", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientReceive", "home/devices/+/availability", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientReceive", "home/devices/+/states/system", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientReceive", "home/devices/+/states/capabilities", cancellationToken);
        await ReplaceAclAsync(BackendRole, "publishClientReceive", "home/devices/+/command/result", cancellationToken);
        await ReplaceAclAsync(BackendRole, "subscribeLiteral", "home/provision/+/request", cancellationToken);
        await ReplaceAclAsync(BackendRole, "subscribeLiteral", "home/devices/+/availability", cancellationToken);
        await ReplaceAclAsync(BackendRole, "subscribeLiteral", "home/devices/+/states/system", cancellationToken);
        await ReplaceAclAsync(BackendRole, "subscribeLiteral", "home/devices/+/states/capabilities", cancellationToken);
        await ReplaceAclAsync(BackendRole, "subscribeLiteral", "home/devices/+/command/result", cancellationToken);
        await ReplaceAclAsync(BackendRole, "unsubscribeLiteral", "home/provision/+/request", cancellationToken);
        await ReplaceAclAsync(BackendRole, "unsubscribeLiteral", "home/devices/+/availability", cancellationToken);
        await ReplaceAclAsync(BackendRole, "unsubscribeLiteral", "home/devices/+/states/system", cancellationToken);
        await ReplaceAclAsync(BackendRole, "unsubscribeLiteral", "home/devices/+/states/capabilities", cancellationToken);
        await ReplaceAclAsync(BackendRole, "unsubscribeLiteral", "home/devices/+/command/result", cancellationToken);

        await EnsureRoleAsync(DeviceRole, cancellationToken);
        await ReplaceAclAsync(DeviceRole, "publishClientSend", "home/devices/%u/availability", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "publishClientSend", "home/devices/%u/states/system", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "publishClientSend", "home/devices/%u/states/capabilities", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "publishClientSend", "home/devices/%u/command/result", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "publishClientReceive", "home/devices/%u/command", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "subscribePattern", "home/devices/%u/command", cancellationToken);
        await ReplaceAclAsync(DeviceRole, "unsubscribePattern", "home/devices/%u/command", cancellationToken);
        await EnsureGroupAsync(_options.DeviceGroup, cancellationToken);
        await ReplaceGroupRoleAsync(_options.DeviceGroup, DeviceRole, cancellationToken);

        await EnsureRoleAsync(ProvisioningRole, cancellationToken);
        await ReplaceAclAsync(ProvisioningRole, "publishClientSend", "home/provision/%c/request", cancellationToken);
        await ReplaceAclAsync(ProvisioningRole, "publishClientReceive", "home/provision/%c/response", cancellationToken);
        await ReplaceAclAsync(ProvisioningRole, "subscribePattern", "home/provision/%c/response", cancellationToken);
        await ReplaceAclAsync(ProvisioningRole, "unsubscribePattern", "home/provision/%c/response", cancellationToken);
        await EnsureGroupAsync(ProvisioningGroup, cancellationToken);
        await ReplaceGroupRoleAsync(ProvisioningGroup, ProvisioningRole, cancellationToken);
        await ExecuteRequiredAsync(new JsonObject
        {
            ["command"] = "setAnonymousGroup",
            ["groupname"] = ProvisioningGroup
        }, cancellationToken);

        await UpsertClientAsync(
            _options.BackendUsername,
            _options.BackendPassword,
            _options.BackendClientId,
            roles: new[] { BackendRole },
            groups: Array.Empty<string>(),
            cancellationToken);

        await ExecuteRequiredAsync(new JsonObject
        {
            ["command"] = "setDefaultACLAccess",
            ["acls"] = new JsonArray(
                new[] { "publishClientSend", "publishClientReceive", "subscribe", "unsubscribe" }
                    .Select(aclType => (JsonNode)new JsonObject
                    {
                        ["acltype"] = aclType,
                        ["allow"] = false
                    })
                    .ToArray())
        }, cancellationToken);

        _logger.LogInformation("Mosquitto Dynamic Security roles and backend client are ready.");
    }

    private async Task EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var error = await ExecuteCommandAsync(new JsonObject
        {
            ["command"] = "createRole",
            ["rolename"] = roleName
        }, cancellationToken);

        if (error is not null && !IsAlreadyExistsError(error))
            ThrowIfCommandFailed("createRole", roleName, error);
    }

    private async Task EnsureGroupAsync(string groupName, CancellationToken cancellationToken)
    {
        var error = await ExecuteCommandAsync(new JsonObject
        {
            ["command"] = "createGroup",
            ["groupname"] = groupName
        }, cancellationToken);

        if (error is not null && !IsAlreadyExistsError(error))
            ThrowIfCommandFailed("createGroup", groupName, error);
    }

    private async Task ReplaceAclAsync(
        string roleName,
        string aclType,
        string topic,
        CancellationToken cancellationToken)
    {
        await ExecuteCommandAsync(new JsonObject
        {
            ["command"] = "removeRoleACL",
            ["rolename"] = roleName,
            ["acltype"] = aclType,
            ["topic"] = topic
        }, cancellationToken);

        await ExecuteRequiredAsync(new JsonObject
        {
            ["command"] = "addRoleACL",
            ["rolename"] = roleName,
            ["acltype"] = aclType,
            ["topic"] = topic,
            ["allow"] = true,
            ["priority"] = 100
        }, cancellationToken);
    }

    private async Task ReplaceGroupRoleAsync(
        string groupName,
        string roleName,
        CancellationToken cancellationToken)
    {
        await ExecuteCommandAsync(new JsonObject
        {
            ["command"] = "removeGroupRole",
            ["groupname"] = groupName,
            ["rolename"] = roleName
        }, cancellationToken);

        await ExecuteRequiredAsync(new JsonObject
        {
            ["command"] = "addGroupRole",
            ["groupname"] = groupName,
            ["rolename"] = roleName,
            ["priority"] = 100
        }, cancellationToken);
    }

    private async Task<bool> UpsertClientAsync(
        string username,
        string password,
        string clientId,
        IReadOnlyCollection<string>? roles,
        IReadOnlyCollection<string>? groups,
        CancellationToken cancellationToken)
    {
        var createError = await ExecuteCommandAsync(
            CreateClientCommand("createClient", username, password, clientId, roles, groups),
            cancellationToken);
        if (createError is null)
            return true;
        if (!IsAlreadyExistsError(createError))
            ThrowIfCommandFailed("createClient", username, createError);

        var modifyError = await ExecuteCommandAsync(
            CreateClientCommand("modifyClient", username, password, clientId, roles, groups),
            cancellationToken);
        ThrowIfCommandFailed("modifyClient", username, modifyError);
        return false;
    }

    private static JsonObject CreateClientCommand(
        string commandName,
        string username,
        string password,
        string clientId,
        IReadOnlyCollection<string>? roles,
        IReadOnlyCollection<string>? groups)
    {
        var command = new JsonObject
        {
            ["command"] = commandName,
            ["username"] = username,
            ["password"] = password,
            ["clientid"] = clientId
        };

        if (roles is not null)
        {
            command["roles"] = new JsonArray(roles.Select(role => (JsonNode)new JsonObject
            {
                ["rolename"] = role,
                ["priority"] = 100
            }).ToArray());
        }

        if (groups is not null)
        {
            command["groups"] = new JsonArray(groups.Select(group => (JsonNode)new JsonObject
            {
                ["groupname"] = group,
                ["priority"] = 100
            }).ToArray());
        }

        return command;
    }

    private async Task ExecuteRequiredAsync(JsonObject command, CancellationToken cancellationToken)
    {
        var name = command["command"]?.GetValue<string>() ?? "unknown";
        var error = await ExecuteCommandAsync(command, cancellationToken);
        ThrowIfCommandFailed(name, "broker authorization configuration", error);
    }

    private async Task<string?> ExecuteCommandAsync(
        JsonObject command,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var correlationData = Guid.NewGuid().ToString("N");
        command["correlationData"] = correlationData;

        var completion = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingCommands.TryAdd(correlationData, completion))
            throw new InvalidOperationException("Unable to track Dynamic Security command.");

        try
        {
            var request = new JsonObject
            {
                ["commands"] = new JsonArray(command)
            };
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(CommandTopic)
                .WithPayload(request.ToJsonString())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message, cancellationToken);

            return await completion.Task.WaitAsync(
                TimeSpan.FromSeconds(_options.CommandTimeoutSeconds),
                cancellationToken);
        }
        finally
        {
            _pendingCommands.TryRemove(correlationData, out _);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
            return;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_client.IsConnected)
                return;

            await _client.ConnectAsync(_clientOptions, cancellationToken);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter
                    .WithTopic(ResponseTopic)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .Build();
            await _client.SubscribeAsync(subscribeOptions, cancellationToken);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private Task HandleApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        if (!string.Equals(eventArgs.ApplicationMessage.Topic, ResponseTopic, StringComparison.Ordinal))
            return Task.CompletedTask;

        var payload = eventArgs.ApplicationMessage.PayloadSegment.Array is not null
            ? Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment)
            : string.Empty;

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("responses", out var responses))
                return Task.CompletedTask;

            foreach (var response in responses.EnumerateArray())
            {
                if (!response.TryGetProperty("correlationData", out var correlationElement))
                    continue;

                var correlationData = correlationElement.GetString();
                if (correlationData is null || !_pendingCommands.TryGetValue(correlationData, out var completion))
                    continue;

                var error = response.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetString()
                    : null;
                completion.TrySetResult(error);
            }
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid Mosquitto Dynamic Security response payload.");
        }

        return Task.CompletedTask;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("MqttDynamicSecurity:Host is required.");
        if (_options.Port is <= 0 or > 65535)
            throw new InvalidOperationException("MqttDynamicSecurity:Port is invalid.");
        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("MqttDynamicSecurity:ClientId is required.");
        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("MqttDynamicSecurity:Username is required.");
        if (string.IsNullOrEmpty(_options.Password))
            throw new InvalidOperationException("MqttDynamicSecurity:Password is required.");
        if (string.IsNullOrWhiteSpace(_options.BackendUsername))
            throw new InvalidOperationException("MqttDynamicSecurity:BackendUsername is required.");
        if (string.IsNullOrEmpty(_options.BackendPassword))
            throw new InvalidOperationException("MqttDynamicSecurity:BackendPassword is required.");
        if (string.IsNullOrWhiteSpace(_options.BackendClientId))
            throw new InvalidOperationException("MqttDynamicSecurity:BackendClientId is required.");
        if (string.IsNullOrWhiteSpace(_options.DeviceGroup))
            throw new InvalidOperationException("MqttDynamicSecurity:DeviceGroup is required.");
        if (_options.CommandTimeoutSeconds <= 0)
            throw new InvalidOperationException("MqttDynamicSecurity:CommandTimeoutSeconds must be positive.");
    }

    private static bool IsNotFoundError(string error)
    {
        return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAlreadyExistsError(string error)
    {
        return error.Contains("already exists", StringComparison.OrdinalIgnoreCase);
    }

    private static void ThrowIfCommandFailed(string command, string username, string? error)
    {
        if (error is null)
            return;

        throw new InvalidOperationException(
            $"Mosquitto Dynamic Security command '{command}' failed for '{username}': {error}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _client.ApplicationMessageReceivedAsync -= HandleApplicationMessage;
        _client.Dispose();
        _connectLock.Dispose();

        foreach (var completion in _pendingCommands.Values)
            completion.TrySetCanceled();
        _pendingCommands.Clear();
    }
}
