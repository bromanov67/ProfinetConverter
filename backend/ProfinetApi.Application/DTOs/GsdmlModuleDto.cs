namespace ProfinetApi.Application.DTOs
{
    public record GsdmlModuleDto(
        string Id,
        string? Name,
        string? Info,
        List<int> AllowedInSlots 
    );
}
