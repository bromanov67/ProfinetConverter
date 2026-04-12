using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfinetApi.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; private set; }

    // Единый список для всех типов серверов (Profinet, IEC 104 и т.д.)
    public List<ServerBase> Servers { get; private set; } = new();

    public Project(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    // Универсальное добавление любого сервера (наследника ServerBase)
    public void AddServer(ServerBase server)
    {
        server.ProjectId = this.Id;
        Servers.Add(server);
    }

    // Главный метод для рекурсивного удаления любого узла (Сервера, Интерфейса, Станции и т.д.)
    public bool RemoveNodeRecursive(Guid nodeId)
    {
        // 1. Сначала проверяем: может быть удалить нужно сам сервер?
        var server = Servers.FirstOrDefault(s => s.Id == nodeId);
        if (server != null)
        {
            Servers.Remove(server);
            return true;
        }

        // 2. Если это не сервер, спускаемся глубже (делегируем удаление самому серверу)
        foreach (var s in Servers)
        {
            // У ServerBase есть свой метод RemoveNode, который будет искать внутри своих интерфейсов
            if (s.RemoveNode(nodeId))
                return true;
        }

        return false;
    }
}
