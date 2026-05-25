using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Infrastructure.Message.Mqtt.Dtos;

namespace Infrastructure.Message.Mqtt;

internal static class MqttJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        options.TypeInfoResolverChain.Add(MqttMessageJsonContext.Default);
        options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());

        return options;
    }
}
