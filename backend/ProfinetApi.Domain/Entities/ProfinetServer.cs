namespace ProfinetApi.Domain.Entities;

public class ProfinetServer : ProfinetNode
{
    public bool Active { get; set; } = true;
    public int Address { get; set; } = 1;
    public string ProtocolVersion { get; set; } = "V0";
    public string MasterClass { get; set; } = "Class 1";
    public List<NetworkInterface> Interfaces { get; private set; } = new();

    public ProfinetServer(string name) : base(name, "server") { }

    public void AddInterface(NetworkInterface netInterface) => Interfaces.Add(netInterface);

    public bool RemoveNode(Guid id)
    {
        // 1. Проверяем, не этот ли интерфейс нужно удалить
        var iface = Interfaces.FirstOrDefault(i => i.Id == id);
        if (iface != null)
        {
            Interfaces.Remove(iface);
            return true;
        }

        // 2. Иначе ищем внутри интерфейсов (удаляем станцию)
        foreach (var item in Interfaces)
        {
            if (item.RemoveNode(id)) return true;
        }

        return false;
    }
}
