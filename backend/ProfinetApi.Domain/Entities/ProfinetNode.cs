namespace ProfinetApi.Domain.Entities;

public abstract class ProfinetNode
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public string Type { get; protected set; }

    protected ProfinetNode(string name, string type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
    }
}
