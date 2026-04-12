namespace ProfinetApi.Domain.Entities.IEC104;

public class IecChannel
{
    public Guid Id { get; set; }
    public Guid InterfaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public string IpAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 2404;
    public string Timezone { get; set; } = "UTC";
    public int K { get; set; } = 12;
    public int W { get; set; } = 8;
    public int T0 { get; set; } = 30;
    public int T1 { get; set; } = 15;
    public int T2 { get; set; } = 10;
    public int T3 { get; set; } = 20;
    public bool Buffering { get; set; } = true;
    public int BufferTi { get; set; } = 25;
    public int BufferTs { get; set; } = 1000;

    public string ConfigurationData { get; set; } = "";
}
