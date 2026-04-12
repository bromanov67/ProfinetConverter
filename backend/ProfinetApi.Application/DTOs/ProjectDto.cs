using System;
using System.Collections.Generic;

namespace ProfinetApi.Application.DTOs
{
    public record ProjectDto(
        Guid Id,
        string Name,
        string Type,
        DateTime CreatedAt,
        IEnumerable<object> Servers
    );
}
