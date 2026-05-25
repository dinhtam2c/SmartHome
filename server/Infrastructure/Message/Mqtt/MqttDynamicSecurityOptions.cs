namespace Infrastructure.Message.Mqtt;

public sealed class MqttDynamicSecurityOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1883;

    public string ClientId { get; set; } = "server-dynsec-admin";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string BackendUsername { get; set; } = "smarthome-backend";

    public string BackendPassword { get; set; } = string.Empty;

    public string BackendClientId { get; set; } = "server";

    public string DeviceGroup { get; set; } = "smarthome-devices";

    public int CommandTimeoutSeconds { get; set; } = 10;
}
