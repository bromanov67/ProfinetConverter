namespace ProfinetApi.Application.DTOs
{
    public record ProjectDto(
     Guid Id,
     string Name,
     string Type,
     DateTime CreatedAt,
     List<ProfinetServerDto> Servers
 );

}
