namespace ProfinetApi.Application.DTOs
{
    public record IecNetworkInterfaceDto(
        Guid Id,
        string Name,
        string Type,
        bool Active,
        string Description,
        List<IecChannelDto> Channels
    );
}
