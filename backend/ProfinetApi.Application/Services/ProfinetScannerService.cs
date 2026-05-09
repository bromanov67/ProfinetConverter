using PacketDotNet;
using ProfinetApi.Application.Interfaces;
using ProfinetApi.Domain.Entities.Profinet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Text;

namespace ProfinetApi.Application.Services
{
    public class ProfinetScannerService : IProfinetScannerService
    {
        private ILiveDevice _device;
        private readonly ConcurrentDictionary<string, ProfinetDeviceRequest> _discoveredRequests = new();

        public void StartScanning(string interfaceName)
        {
            if (_device != null && _device.Started)
                return;

            var devices = LibPcapLiveDeviceList.Instance;

            _device = devices.FirstOrDefault(d =>
                (d.Name != null && d.Name.Contains(interfaceName, StringComparison.OrdinalIgnoreCase)) ||
                (d.Description != null && d.Description.Contains(interfaceName, StringComparison.OrdinalIgnoreCase)));

            if (_device == null)
            {
                var available = string.Join(", ", devices.Select(d => d.Name));
                throw new Exception($"Интерфейс '{interfaceName}' не найден. Доступные: {available}");
            }

            _discoveredRequests.Clear();

            _device.OnPacketArrival += Device_OnPacketArrival;
            _device.Open(DeviceModes.Promiscuous | DeviceModes.MaxResponsiveness, read_timeout: 1000);

            _device.StartCapture();
            Console.WriteLine($"[DCP МАСТЕР] Начато прослушивание сети на: {_device.Description}");
        }

        // === НОВЫЙ МЕТОД МАСТЕРА: Отправляем запрос на поиск всех устройств ===
        public void SendIdentifyRequest()
        {
            if (_device == null || !_device.Started) return;

            // Мультикаст MAC-адрес PROFINET для DCP запросов
            var destMac = PhysicalAddress.Parse("01-0E-CF-00-00-00");

            // Если библиотека не отдает MAC, используем заглушку, 
            // но в норме _device.MacAddress содержит физический MAC вашей сетевой карты.
            var srcMac = _device.MacAddress ?? PhysicalAddress.Parse("00-11-22-33-44-55");

            // Формируем payload для Identify Request (All Devices)
            byte[] payload = new byte[] {
                0xFE, 0xFE, // Frame ID: 0xFEFE (Identify Request)
                0x05, 0x00, // Service ID: 5 (Identify), Service Type: 0 (Request)
                0x00, 0x00, 0x00, 0x01, // XID (Transaction ID)
                0x00, 0x80, // ResponseDelay (128)
                0x00, 0x04, // DCPDataLength (4 байта данных ниже)
                0xFF, 0xFF, // Option: All (255), Suboption: All (255)
                0x00, 0x00  // BlockLength: 0
            };

            var eth = new EthernetPacket(srcMac, destMac, (EthernetType)0x8892)
            {
                PayloadData = payload
            };

            _device.SendPacket(eth);
            Console.WriteLine("[DCP МАСТЕР] Отправлен широковещательный запрос Identify Request (Поиск всех устройств)...");
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var eth = packet.Extract<EthernetPacket>();

            if (eth != null && eth.Type == (EthernetType)0x8892)
            {
                ParseProfinetDcpPacket(eth);
            }
        }

        private void ParseProfinetDcpPacket(EthernetPacket eth)
        {
            byte[] payload = eth.PayloadData;
            if (payload.Length < 12) return;

            ushort frameId = (ushort)((payload[0] << 8) | payload[1]);

            // === ТЕПЕРЬ МЫ ЛОВИМ ОТВЕТЫ (0xFEFF) ОТ СЛЕЙВОВ ===
            if (frameId == 0xFEFF)
            {
                byte serviceId = payload[2];
                byte serviceType = payload[3];

                // Нас интересуют ответы (ServiceId: 5 = Identify, ServiceType: 1 = Response)
                if (serviceId != 5 || serviceType != 1) return;

                string deviceName = "Unknown";
                string ipAddress = "0.0.0.0";

                int offset = 12;

                while (offset + 4 <= payload.Length)
                {
                    byte option = payload[offset];
                    byte suboption = payload[offset + 1];
                    ushort blockLength = (ushort)((payload[offset + 2] << 8) | payload[offset + 3]);

                    if (offset + 4 + blockLength > payload.Length) break;

                    // Извлекаем имя устройства (Option 2, Sub 2)
                    if (option == 2 && suboption == 2)
                    {
                        int nameLen = blockLength - 2;
                        if (nameLen > 0)
                        {
                            deviceName = Encoding.ASCII.GetString(payload, offset + 6, nameLen).Trim('\0');
                        }
                    }
                    // Извлекаем IP-адрес (Option 1, Sub 2)
                    else if (option == 1 && suboption == 2 && blockLength >= 14)
                    {
                        // В этом блоке IP адрес начинается со смещения +6 от начала блока
                        ipAddress = $"{payload[offset + 6]}.{payload[offset + 7]}.{payload[offset + 8]}.{payload[offset + 9]}";
                    }

                    offset += 4 + blockLength;
                    if (offset % 2 != 0) offset++; // Выравнивание
                }

                // Добавляем найденный слейв в наш список
                AddDiscoveredDevice(deviceName, eth.SourceHardwareAddress.ToString(), ipAddress);
            }
        }

        private void AddDiscoveredDevice(string name, string mac, string ip)
        {
            _discoveredRequests.AddOrUpdate(
                mac, // Теперь ключ - это MAC адрес
                new ProfinetDeviceRequest { RequestedName = name, MacAddress = mac, LastSeen = DateTime.Now },
                (key, oldValue) => { oldValue.LastSeen = DateTime.Now; oldValue.RequestedName = name; return oldValue; });

            Console.WriteLine($"[DCP МАСТЕР] Найден Слейв: Имя='{name}', MAC={mac}, IP={ip}");
        }

        public List<ProfinetDeviceRequest> GetDiscoveredRequests()
        {
            // Очищаем старые устройства, которые не отзывались более 15 секунд
            var threshold = DateTime.Now.AddSeconds(-15);
            foreach (var key in _discoveredRequests.Keys)
            {
                if (_discoveredRequests[key].LastSeen < threshold)
                {
                    _discoveredRequests.TryRemove(key, out _);
                }
            }

            return _discoveredRequests.Values.ToList();
        }

        public void StopScanning()
        {
            if (_device != null)
            {
                if (_device.Started) _device.StopCapture();
                _device.OnPacketArrival -= Device_OnPacketArrival;
                _device.Close();
                _device = null;
                Console.WriteLine("[SCANNER] Прослушивание остановлено.");
            }
        }
    }
}