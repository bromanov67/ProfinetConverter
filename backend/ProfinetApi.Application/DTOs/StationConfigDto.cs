namespace ProfinetApi.Application.DTOs
{
    public record StationConfigDto(
        string Id, string Manufacturer, string Model, string Version,
        string ShortDesignation, string DeviceDescription, string ArticleNo,
        string FirmwareVersion, string HardwareVersion, string GsdFile,
        string ProfinetDeviceName, string IpAddress, string SubnetMask,
        int DeviceNumber, string Consistency,
        List<GsdmlSlotDto> Slots,
        List<GsdmlModuleDto> Modules
    );
}
