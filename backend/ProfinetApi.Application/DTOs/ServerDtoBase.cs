using System;
using System.Text.Json.Serialization;

namespace ProfinetApi.Application.DTOs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ProfinetServerDto), "server_profinet")]
    [JsonDerivedType(typeof(Iec104ServerDto), "server_iec104")]
    public abstract record ServerDtoBase(
        Guid Id,
        string Name,
        bool Active,
        string Description
    );
}
