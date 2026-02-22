namespace ProfinetApi.Domain.Entities;

public class Station : ProfinetNode
{
    public bool Active { get; set; } = true;
    public int Address { get; set; }
    public string Description { get; set; } = string.Empty;
    public string GsdmlContent { get; set; } = string.Empty;
    public string ConfigIdentifier { get; set; } = "";
    public string ConfigManufacturer { get; set; } = "";
    public string ConfigModel { get; set; } = "";
    public string ConfigVersion { get; set; } = "";
    public string ConfigurationData { get; set; } = "";

    public Station(string name, int address) : base(name, "station")
    {
        Address = address;
    }
}
