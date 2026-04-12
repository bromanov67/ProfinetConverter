namespace ProfinetApi.Domain.Entities.Profinet;

public class ProfinetInterface : InterfaceBase
{
    public List<Station> Stations { get; set; } = new();

    public override bool RemoveNode(Guid id)
    {
        var station = Stations.FirstOrDefault(s => s.Id == id);
        if (station != null)
        {
            Stations.Remove(station);
            return true;
        }

        return false;
    }
}
