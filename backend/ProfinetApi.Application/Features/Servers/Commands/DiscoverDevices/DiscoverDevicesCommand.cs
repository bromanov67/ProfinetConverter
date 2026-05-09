using MediatR;
using ProfinetApi.Application.Interfaces;

namespace ProfinetApi.Application.Features.Servers.Commands.DiscoverDevices
{
    // DTO для найденного запроса от ПЛК
    public record DiscoveredDeviceDto(string RequestedName, string MacAddress, DateTime LastSeen);

    // Команда принимает имя (или GUID) интерфейса
    public record DiscoverDevicesCommand(string InterfaceName) : IRequest<DiscoveryResult>;

    // Результат теперь возвращает СПИСОК найденных имен
    public record DiscoveryResult(bool Success, List<DiscoveredDeviceDto> Devices, string Message);

    public class DiscoverDevicesCommandHandler : IRequestHandler<DiscoverDevicesCommand, DiscoveryResult>
    {
        private readonly IProfinetScannerService _scannerService;

        public DiscoverDevicesCommandHandler(IProfinetScannerService scannerService)
        {
            _scannerService = scannerService;
        }

        public async Task<DiscoveryResult> Handle(DiscoverDevicesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Начинаем слушать интерфейс
                _scannerService.StartScanning(request.InterfaceName);

                // 2. ВОТ ЗДЕСЬ МЫ КРИЧИМ В СЕТЬ (Этой строки не было!)
                _scannerService.SendIdentifyRequest();
            }
            catch (Exception ex)
            {
                return new DiscoveryResult(false, new List<DiscoveredDeviceDto>(), $"Ошибка запуска сканера: {ex.Message}");
            }

            try
            {
                // 3. Ждем ответы (10 секунд для DCP это очень много, обычно хватает 2-3 секунд)
                await Task.Delay(3000, cancellationToken);

                var foundRequests = _scannerService.GetDiscoveredRequests();

                if (foundRequests.Count == 0)
                {
                    return new DiscoveryResult(false, new List<DiscoveredDeviceDto>(), "Запросы от ПЛК не обнаружены.");
                }

                var dtos = foundRequests.Select(r => new DiscoveredDeviceDto(r.RequestedName, r.MacAddress, r.LastSeen)).ToList();
                return new DiscoveryResult(true, dtos, $"Успешно! Найдено {dtos.Count} устройств(а).");
            }
            finally
            {
                _scannerService.StopScanning();
            }
        }
    }
}