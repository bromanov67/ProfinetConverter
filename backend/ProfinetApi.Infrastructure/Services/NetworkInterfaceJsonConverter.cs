using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProfinetApi.API.Serialization;

public class NetworkInterfaceJsonConverter : JsonConverter<InterfaceBase>
{
    public override InterfaceBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var rawJson = root.GetRawText();

        if (root.TryGetProperty("stations", out _))
        {
            return JsonSerializer.Deserialize<ProfinetInterface>(rawJson, options)
                   ?? throw new JsonException("Failed to deserialize ProfinetInterface.");
        }

        if (root.TryGetProperty("channels", out _))
        {
            return JsonSerializer.Deserialize<IecInterface>(rawJson, options)
                   ?? throw new JsonException("Failed to deserialize Iec104Interface.");
        }

        return JsonSerializer.Deserialize<InterfaceBase>(rawJson, options)
               ?? throw new JsonException("Failed to deserialize NetworkInterface.");
    }

    public override void Write(Utf8JsonWriter writer, InterfaceBase value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ProfinetInterface profinetInterface:
                JsonSerializer.Serialize(writer, profinetInterface, options);
                break;

            case IecInterface iec104Interface:
                JsonSerializer.Serialize(writer, iec104Interface, options);
                break;

            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }
}