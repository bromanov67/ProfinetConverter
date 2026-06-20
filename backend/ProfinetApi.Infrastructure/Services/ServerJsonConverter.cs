using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProfinetApi.API.Serialization;

public class ServerJsonConverter : JsonConverter<ServerBase>
{
    public override ServerBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
            throw new JsonException("Server type discriminator is missing.");

        var rawJson = root.GetRawText();
        var serverType = typeProperty.GetInt32();

        return serverType switch
        {
            0 => JsonSerializer.Deserialize<ProfinetServer>(rawJson, options)
                 ?? throw new JsonException("Failed to deserialize ProfinetServer."),
            1 => JsonSerializer.Deserialize<IecServer>(rawJson, options)
                 ?? throw new JsonException("Failed to deserialize Iec104Server."),
            _ => throw new JsonException($"Unsupported server type: {serverType}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ServerBase value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ProfinetServer profinetServer:
                JsonSerializer.Serialize(writer, profinetServer, options);
                break;

            case IecServer iec104Server:
                JsonSerializer.Serialize(writer, iec104Server, options);
                break;

            default:
                throw new JsonException($"Unsupported server runtime type: {value.GetType().Name}.");
        }
    }
}