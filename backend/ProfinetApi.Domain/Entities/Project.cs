namespace ProfinetApi.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; private set; }
    public List<ProfinetServer> Servers { get; private set; } = new();

    public Project(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddServer(ProfinetServer server) => Servers.Add(server);

    // Главный метод для рекурсивного удаления любого узла
    public bool RemoveNodeRecursive(Guid nodeId)
    {
        // 1. Проверяем серверы
        var server = Servers.FirstOrDefault(s => s.Id == nodeId);
        if (server != null)
        {
            Servers.Remove(server);
            return true;
        }

        // 2. Ищем глубже
        foreach (var s in Servers)
        {
            if (s.RemoveNode(nodeId)) return true;
        }

        return false;
    }
}
