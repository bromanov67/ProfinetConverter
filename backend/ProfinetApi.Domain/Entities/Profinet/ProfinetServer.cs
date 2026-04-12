namespace ProfinetApi.Domain.Entities.Profinet;

public class ProfinetServer : ServerBase
{
    public override ServerType Type => ServerType.Profinet;

    // Специфичные поля для Profinet
    public int Address { get; set; } = 1;
    public string ProtocolVersion { get; set; } = "V0";
    public string MasterClass { get; set; } = "Class 1";

    // Удобный метод для получения именно профинет-интерфейсов (если нужно на бэкенде)
    public IEnumerable<ProfinetInterface> ProfinetInterfaces => Interfaces.OfType<ProfinetInterface>();
}
