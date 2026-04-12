namespace ProfinetApi.Infrastructure.Services
{
    public interface IProfinetRuntimeService
    {
        bool IsRunning { get; }
        Task StartServerAsync(string interfaceName, string stationName);
        Task StopServerAsync();

        // Метод для будущей отправки команд из C# в C++
        Task WriteOutputAsync(int offset, byte[] data);
    }
}