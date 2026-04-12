using lib60870.CS101;
using lib60870.CS104;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using ProfinetApi.Application.Services;
using ProfinetApi.Infrastructure.Hubs;
using System.Collections.Concurrent;

namespace ProfinetApi.Infrastructure.Services
{
    public class SignalData
    {
        public int IOA { get; set; }
        public string Identifier { get; set; }
        public string Value { get; set; }
        public string Quality { get; set; }
        public string Type { get; set; }
    }

    public class Iec104RuntimeService : IIec104RuntimeService, IHostedService
    {
        private readonly IHubContext<RuntimeHub> _hubContext;
        private System.Timers.Timer _broadcastTimer;
        private Server _server;

        private ConcurrentDictionary<int, SignalData> _plcMemory = new();

        public Iec104RuntimeService(IHubContext<RuntimeHub> hubContext)
        {
            _hubContext = hubContext;
            InitPlcMemory();
        }

        private void InitPlcMemory()
        {
            // Две булевские переменные
            _plcMemory.TryAdd(101, new SignalData { IOA = 101, Identifier = "Signal_101", Value = "1", Quality = "GOOD", Type = "bool" });
            _plcMemory.TryAdd(102, new SignalData { IOA = 102, Identifier = "Signal_102", Value = "1", Quality = "GOOD", Type = "bool" });

            _plcMemory.TryAdd(103, new SignalData { IOA = 103, Identifier = "Signal_103_Float", Value = "15.5", Quality = "GOOD", Type = "float" });
        }

        public bool IsRunning => _server != null && _server.IsRunning();

        public void StartServer(string ip, int port)
        {
            if (IsRunning) return;

            _server = new Server();

            // 1. ОБРАБОТЧИК ЧТЕНИЯ (Общий опрос)
            _server.SetInterrogationHandler((parameter, connection, asdu, qoi) =>
            {
                if (qoi == 20)
                {
                    connection.SendACT_CON(asdu, false);

                    var appParams = connection.GetApplicationLayerParameters();
                    var goodQuality = new QualityDescriptor();

                    // --- 1. Отправляем Булевские переменные (bool) ---
                    var boolSignals = _plcMemory.Values.Where(s => s.Type == "bool").ToList();
                    if (boolSignals.Any())
                    {
                        var boolAsdu = new ASDU(appParams, CauseOfTransmission.INTERROGATED_BY_STATION, false, false, 0, asdu.Ca, false);
                        foreach (var sig in boolSignals)
                        {
                            boolAsdu.AddInformationObject(new SinglePointInformation(sig.IOA, sig.Value == "1", goodQuality));
                        }
                        connection.SendASDU(boolAsdu);
                    }

                    // --- 2. Отправляем Аналоговые переменные (float) ---
                    var floatSignals = _plcMemory.Values.Where(s => s.Type == "float").ToList();
                    if (floatSignals.Any())
                    {
                        // Для float используется другой тип - MeasuredValueShort (M_ME_NC_1)
                        var floatAsdu = new ASDU(appParams, CauseOfTransmission.INTERROGATED_BY_STATION, false, false, 0, asdu.Ca, false);
                        foreach (var sig in floatSignals)
                        {
                            if (float.TryParse(sig.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                            {
                                floatAsdu.AddInformationObject(new MeasuredValueShort(sig.IOA, floatVal, goodQuality));
                            }
                        }
                        connection.SendASDU(floatAsdu);
                    }

                    connection.SendACT_TERM(asdu);
                    return true;
                }
                return false;
            }, null);

            // 2. ОБРАБОТЧИК ЗАПИСИ (Команды от MasterOPC)
            _server.SetASDUHandler((parameter, connection, asdu) =>
            {
                // Запись bool (одиночная команда)
                if (asdu.TypeId == TypeID.C_SC_NA_1)
                {
                    var cmd = (SingleCommand)asdu.GetElement(0);
                    if (_plcMemory.TryGetValue(cmd.ObjectAddress, out var sig) && sig.Type == "bool")
                    {
                        sig.Value = cmd.State ? "1" : "0";
                        connection.SendACT_CON(asdu, false);
                        return true;
                    }
                }
                // НОВОЕ: Запись float (команда установки значения / Set point command)
                else if (asdu.TypeId == TypeID.C_SE_NC_1)
                {
                    var cmd = (SetpointCommandShort)asdu.GetElement(0);
                    if (_plcMemory.TryGetValue(cmd.ObjectAddress, out var sig) && sig.Type == "float")
                    {
                        // Сохраняем с точкой для универсальности
                        sig.Value = cmd.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        connection.SendACT_CON(asdu, false);
                        return true;
                    }
                }

                return true;
            }, null);

            _server.Start();

            // 3. ТАЙМЕР ДЛЯ ФРОНТЕНДА VUE
            _broadcastTimer = new System.Timers.Timer(1000);
            _broadcastTimer.Elapsed += async (sender, e) =>
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRuntimeData", _plcMemory.Values);
            };
            _broadcastTimer.Start();
        }

        public void StopServer()
        {
            _server?.Stop();
            _broadcastTimer?.Stop();
            _broadcastTimer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopServer();
            return Task.CompletedTask;
        }
    }
}