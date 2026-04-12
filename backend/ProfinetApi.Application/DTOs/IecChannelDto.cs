using System;

namespace ProfinetApi.Application.DTOs
{
    public record IecChannelDto(
        Guid Id,
        string Name,
        string Type,
        bool Active,
        string Description,
        object Configuration
    );
}
