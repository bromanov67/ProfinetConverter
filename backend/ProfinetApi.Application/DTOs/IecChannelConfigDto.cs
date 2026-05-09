public class IecChannelConfigDto
{
    public List<IecCommandDto> Commands { get; set; } = new();
    public List<IecSignalDto> Signals { get; set; } = new();
}