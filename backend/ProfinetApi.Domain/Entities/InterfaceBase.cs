namespace ProfinetApi.Domain.Entities;

public abstract class InterfaceBase
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;

    public abstract bool RemoveNode(Guid id);
}
