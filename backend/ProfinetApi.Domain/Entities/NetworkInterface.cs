namespace ProfinetApi.Domain.Entities;

public class NetworkInterface : ProfinetNode
{
    public bool Active { get; set; } = true;

    public List<Station> Stations { get; private set; } = new();

    public NetworkInterface(string name) : base(name, "interface") { }

    public void AddStation(Station station) => Stations.Add(station);

    // Возвращает true, если удаление прошло успешно
    public bool RemoveNode(Guid id)
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
