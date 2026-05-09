using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProfinetServerDto), "server_profinet")]
[JsonDerivedType(typeof(Iec104ServerDto), "server_iec104")]
public abstract record ServerDtoBase(
        Guid Id,
        string Name,
        bool Active,
        string Description
    );