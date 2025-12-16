namespace Application.Common.Message;

public static class MessageTopics
{
    public const string GatewayProvision = "home/gateways/+/provision/request";
    public const string DeviceProvision = "home/gateways/+/devices/+/provision/request";
    public const string GatewayAvailability = "home/gateways/+/availability";
    public const string DeviceAvailability = "home/gateways/+/devices/+/availability";
    public const string DeviceData = "home/gateways/+/data";

    public static string GatewayProvisionRequest(string gatewayId)
        => $"home/gateways/{gatewayId}/provision/request";

    public static string GatewayProvisionResponse(string gatewayId)
        => $"home/gateways/{gatewayId}/provision/response";

    public static string DeviceProvisionRequest(string gatewayId, string deviceId)
        => $"home/gateways/{gatewayId}/devices/{deviceId}/provision/request";

    public static string DeviceProvisionResponse(string gatewayId, string deviceId)
        => $"home/gateways/{gatewayId}/devices/{deviceId}/provision/response";

    public static string DeviceCommand(string gatewayId, string deviceId)
        => $"home/gateways/{gatewayId}/devices/{deviceId}/command";
}
