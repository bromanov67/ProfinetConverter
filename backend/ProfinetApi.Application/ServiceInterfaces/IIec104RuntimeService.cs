namespace ProfinetApi.Application.ServiceInterfaces
{
    public interface IIec104RuntimeService
    {
        void StartServer(string ip, int port, IEnumerable<SignalData> config);
        void StopServer();
        void UpdateMemoryFromProfinet(byte[] payloadBytes);
    }
}