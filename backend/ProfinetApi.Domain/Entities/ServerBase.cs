namespace ProfinetApi.Domain.Entities;

public abstract class ServerBase
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public abstract ServerType Type { get; }

    // ВОТ ЭТА СТРОКА БЫЛА УТЕРЯНА. Она обязательна!
    public List<InterfaceBase> Interfaces { get; set; } = new();

    public void AddInterface(InterfaceBase netInterface) => Interfaces.Add(netInterface);

    public bool RemoveNode(Guid id)
    {
        // 1. Проверяем, не этот ли интерфейс нужно удалить
        var iface = Interfaces.FirstOrDefault(i => i.Id == id);
        if (iface != null)
        {
            Interfaces.Remove(iface);
            return true;
        }

        // 2. Иначе ищем внутри интерфейсов (например, удаляем станцию или канал)
        foreach (var item in Interfaces)
        {
            if (item.RemoveNode(id)) return true;
        }

        return false;
    }
}
