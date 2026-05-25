namespace Infrastructure.Message.Mqtt;

public static class MqttTopics
{
    public const string DeviceProvision = "home/provision/+/request";
    public const string DeviceAvailability = "home/devices/+/availability";
    public const string DeviceSystemState = "home/devices/+/states/system";
    public const string DeviceCapabilitiesState = "home/devices/+/states/capabilities";
    public const string DeviceCommandResult = "home/devices/+/command/result";

    public static string DeviceProvisionResponse(string macAddress)
        => $"home/provision/{macAddress}/response";

    public static string DeviceCommand(Guid deviceId)
        => $"home/devices/{deviceId}/command";
}
