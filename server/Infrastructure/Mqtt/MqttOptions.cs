namespace Infrastructure.Mqtt;

public class MqttOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1883;

    public string ClientId { get; set; } = "Server";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseTls { get; set; } = false;

    public int AutoReconnectDelay { get; set; } = 5;
}
