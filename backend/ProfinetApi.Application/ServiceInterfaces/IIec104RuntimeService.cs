namespace ProfinetApi.Application.ServiceInterfaces
{
    public interface IIec104RuntimeService
    {
        bool IsRunning { get; }
        void StartServer(string ip, int port, IEnumerable<SignalData> config);
        void StopServer();
        void UpdateMemoryFromProfinet(byte[] payloadBytes);
    }
}