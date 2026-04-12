namespace ProfinetApi.Application.Services
{
    public interface IIec104RuntimeService
    {
        void StartServer(string ip, int port);
        void StopServer();
        bool IsRunning { get; }
    }
}