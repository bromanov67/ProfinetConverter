using System;
using System.Collections.Generic;
using System.Linq;

namespace ProfinetApi.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; private set; }

    public List<ServerBase> Servers { get; private set; } = new();

    public Project(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddServer(ServerBase server)
    {
        server.ProjectId = this.Id;
        Servers.Add(server);
    }

    public bool RemoveNodeRecursive(Guid nodeId)
    {
        var server = Servers.FirstOrDefault(s => s.Id == nodeId);
        if (server != null)
        {
            Servers.Remove(server);
            return true;
        }

        foreach (var s in Servers)
        {
            if (s.RemoveNode(nodeId))
                return true;
        }

        return false;
    }
}