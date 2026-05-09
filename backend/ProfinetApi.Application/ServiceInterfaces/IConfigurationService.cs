namespace ProfinetApi.Application.ServiceInterfaces
{
    public interface IConfigurationService
    {
        Task ImportGsdmlAsync(Guid stationId, Stream fileStream, string fileName, CancellationToken ct);
    }
}
