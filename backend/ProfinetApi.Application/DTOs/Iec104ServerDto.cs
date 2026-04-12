namespace ProfinetApi.Application.DTOs
{
    public record Iec104ServerDto(
        Guid Id,
        string Name,
        bool Active,
        string Description,
        List<IecNetworkInterfaceDto> Interfaces
    ) : ServerDtoBase(Id, Name, Active, Description);
}
