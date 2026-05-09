using lib60870.CS101;
using lib60870.CS104;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using ProfinetApi.Application.ServiceInterfaces;
using ProfinetApi.Infrastructure.Hubs;
using System.Collections.Concurrent;

namespace ProfinetApi.Infrastructure.Services
{

    public class Iec104RuntimeService : IIec104RuntimeService, IHostedService
    {
        private readonly IHubContext<RuntimeHub> _hubContext;
        private System.Timers.Timer _broadcastTimer;
        private Server _server;

        private ConcurrentDictionary<int, SignalData> _plcMemory = new();

        public Iec104RuntimeService(IHubContext<RuntimeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public bool IsRunning => _server != null && _server.IsRunning();

        public void StartServer(string ip, int port, IEnumerable<SignalData> config)
        {
            if (IsRunning) return;

            _plcMemory.Clear();
            Console.WriteLine($"[IEC104] StartServer вызван. Конфигов получено: {config?.Count() ?? 0}");

            if (config != null)
            {
                foreach (var item in config)
                {
                    bool added = _plcMemory.TryAdd(item.IOA, new SignalData
                    {
                        IOA = item.IOA,
                        Identifier = item.Identifier,
                        Value = item.Type == "float" ? "0.0" : "0",
                        Quality = "GOOD",
                        Type = item.Type,
                        ByteOffset = item.ByteOffset,
                        BitOffset = item.BitOffset
                    });
                    Console.WriteLine($"[IEC104] Добавлен тег IOA={item.IOA}, Type={item.Type}, ByteOffset={item.ByteOffset}. Успех: {added}");
                }
            }

            Console.WriteLine($"[IEC104] _plcMemory.Count после инициализации = {_plcMemory.Count}");

            _server = new Server();

            _server.SetInterrogationHandler((parameter, connection, asdu, qoi) =>
            {
                if (qoi == 20)
                {
                    connection.SendACT_CON(asdu, false);

                    var appParams = connection.GetApplicationLayerParameters();
                    var goodQuality = new QualityDescriptor();

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

                    var floatSignals = _plcMemory.Values.Where(s => s.Type == "float").ToList();
                    if (floatSignals.Any())
                    {
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

            _server.SetASDUHandler((parameter, connection, asdu) =>
            {
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
                else if (asdu.TypeId == TypeID.C_SE_NC_1)
                {
                    var cmd = (SetpointCommandShort)asdu.GetElement(0);
                    if (_plcMemory.TryGetValue(cmd.ObjectAddress, out var sig) && sig.Type == "float")
                    {
                        sig.Value = cmd.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        connection.SendACT_CON(asdu, false);
                        return true;
                    }
                }

                return true;
            }, null);

            _server.Start();

            _broadcastTimer = new System.Timers.Timer(1000);
            _broadcastTimer.Elapsed += async (sender, e) =>
            {
                await _hubContext.Clients.All.SendAsync("ReceiveRuntimeData", _plcMemory.Values);
            };
            _broadcastTimer.Start();
        }

        public void UpdateMemoryFromProfinet(byte[] payloadBytes)
        {
            foreach (var sig in _plcMemory.Values)
            {
                if (sig.ByteOffset >= payloadBytes.Length)
                    continue;

                if (sig.Type == "bool")
                {
                    byte b = payloadBytes[sig.ByteOffset];
                    bool bitValue = (b & (1 << sig.BitOffset)) != 0;

                    sig.Value = bitValue ? "1" : "0";
                }   
                else if (sig.Type == "int" || sig.Type == "short")
                {
                    if (sig.ByteOffset + 1 < payloadBytes.Length)
                    {
                        short val = BitConverter.ToInt16(payloadBytes, sig.ByteOffset);
                        sig.Value = val.ToString();
                    }
                }
                else if (sig.Type == "float")
                {
                    if (sig.ByteOffset + 3 < payloadBytes.Length)
                    {
                        float val = BitConverter.ToSingle(payloadBytes, sig.ByteOffset);
                        sig.Value = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
            }
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