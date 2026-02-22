namespace ProfinetApi.Application.DTOs
{
    public record GsdmlSlotDto(
        int Number,
        string Label,           
        GsdmlModuleDto? Module
    );
}
