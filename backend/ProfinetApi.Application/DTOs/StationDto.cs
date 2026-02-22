namespace ProfinetApi.Application.DTOs
{
    public record StationDto(
        Guid Id,
        string Name,
        string Type,
        bool Active,
        int Address,
        string Description,
        StationConfigDto Configuration
    );
}
