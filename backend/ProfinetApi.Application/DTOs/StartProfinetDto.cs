public class StartProfinetDto
{
    public string InterfaceName { get; set; } = "enp0s3";
    public string StationName { get; set; } = "PN-Device";
    public int ModuleIdent { get; set; } = 4102;
    public int SubmoduleIdent { get; set; } = 4102;
    public int InputLength { get; set; } = 64;
    public int OutputLength { get; set; } = 64;
    public string IecIpAddress { get; set; } = "127.0.0.1";
    public int IecPort { get; set; } = 2404;
    public List<SignalMappingDto> Signals { get; set; } = new List<SignalMappingDto>();
}