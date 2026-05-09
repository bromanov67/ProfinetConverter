using ProfinetApi.Domain.Entities.Profinet;

namespace ProfinetApi.Application.Interfaces
{
    public interface IProfinetScannerService
    {
        /// <summary>
        /// Запускает фоновое прослушивание сети для поиска DCP запросов от ПЛК.
        /// </summary>
        /// <param name="interfaceName">GUID или имя сетевого адаптера.</param>
        void StartScanning(string interfaceName);


        void SendIdentifyRequest();
        /// <summary>
        /// Возвращает список уникальных устройств, которые в данный момент ищет ПЛК.
        /// </summary>
        List<ProfinetDeviceRequest> GetDiscoveredRequests();

        /// <summary>
        /// Останавливает захват пакетов.
        /// </summary>
        void StopScanning();
    }
}