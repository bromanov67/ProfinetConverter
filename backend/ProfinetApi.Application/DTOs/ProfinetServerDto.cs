namespace ProfinetApi.Application.DTOs
{
    public record ProfinetServerDto(
        Guid Id,
        string Name,
        string Type,
        bool Active,
        int Address,
        List<NetworkInterfaceDto> Interfaces
    );

}
