public record ProjectDto(
       Guid Id,
       string Name,
       string Type,
       DateTime CreatedAt,
       IEnumerable<object> Servers
   );