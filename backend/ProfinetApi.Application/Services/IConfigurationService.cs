namespace ProfinetApi.Application.Services
{
    public interface IConfigurationService
    {
        // Импорт GSDML файла для станции
        Task ImportGsdmlAsync(Guid stationId, Stream fileStream, string fileName, CancellationToken ct);
    }
}
