namespace ProfinetApi.Domain.Entities;

public abstract class ServerBase
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public abstract ServerType Type { get; }

    public List<InterfaceBase> Interfaces { get; set; } = new();

    public void AddInterface(InterfaceBase netInterface) => Interfaces.Add(netInterface);

    public bool RemoveNode(Guid id)
    {
        var iface = Interfaces.FirstOrDefault(i => i.Id == id);
        if (iface != null)
        {
            Interfaces.Remove(iface);
            return true;
        }

        foreach (var item in Interfaces)
        {
            if (item.RemoveNode(id)) return true;
        }

        return false;
    }
}