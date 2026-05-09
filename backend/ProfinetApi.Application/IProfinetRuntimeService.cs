namespace ProfinetApi.Application.Interfaces
{
    public interface IProfinetRuntimeService
    {
        Task StartServerAsync(string interfaceName, string stationName, int moduleIdent, int submoduleIdent, int inputLen, int outputLen);
        Task StopServerAsync();
    }
}