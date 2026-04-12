namespace ProfinetApi.Domain.Entities.Profinet;

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
