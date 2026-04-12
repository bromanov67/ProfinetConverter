namespace ProfinetApi.Domain.Entities.IEC104;

public class IecInterface : InterfaceBase
{
    public List<IecChannel> Channels { get; set; } = new();

    public override bool RemoveNode(Guid id)
    {
        var channel = Channels.FirstOrDefault(c => c.Id == id);
        if (channel != null)
        {
            Channels.Remove(channel);
            return true;
        }
        return false;
    }
}
