public class GsdmlSlotDto
{
    public int Number { get; set; }
    public string Label { get; set; } = "";
    public GsdmlModuleDto? Module { get; set; }
    public List<SignalDto> Signals { get; set; } = new();
    public List<CommandDto> Commands { get; set; } = new();
}