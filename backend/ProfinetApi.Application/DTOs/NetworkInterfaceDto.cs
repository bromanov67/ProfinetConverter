namespace ProfinetApi.Application.DTOs
{
    public record NetworkInterfaceDto(
        Guid Id,
        string Name,
        string Type,
        bool Active, 
        List<StationDto> Stations
    );

}
