public class StationConfigDto
{
    public string? Id { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Version { get; set; }

    public string? ShortDesignation { get; set; }
    public string? DeviceDescription { get; set; }
    public string? ArticleNo { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? HardwareVersion { get; set; }
    public string? GsdFile { get; set; }
    public string? ProfinetDeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? SubnetMask { get; set; }
    public int DeviceNumber { get; set; }
    public string? Consistency { get; set; }

    public List<GsdmlSlotDto> Slots { get; set; } = new();
    public List<GsdmlModuleDto> Modules { get; set; } = new();
}