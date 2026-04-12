using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.SignalR;
using ProfinetApi.GrpcClients;
using ProfinetApi.Infrastructure.Hubs;
using System.Collections.Concurrent;

namespace ProfinetApi.Infrastructure.Services
{
    public class ProfinetRuntimeService : IProfinetRuntimeService
    {
        private readonly IHubContext<RuntimeHub> _hubContext;

        // ИСПРАВЛЕНИЕ 2: Используем новое имя сервиса из proto-файла
        private ProfinetStackService.ProfinetStackServiceClient _grpcClient;
        private GrpcChannel _grpcChannel;
        private AsyncDuplexStreamingCall<IoWriteRequest, IoUpdateResponse> _ioStream;
        private CancellationTokenSource _cts;

        public bool IsRunning { get; private set; }

        private ConcurrentDictionary<int, byte[]> _profinetMemory = new();

        public ProfinetRuntimeService(IHubContext<RuntimeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task StartServerAsync(string interfaceName, string stationName)
        {
            if (IsRunning) return;

            _grpcChannel = GrpcChannel.ForAddress("http://localhost:5001");
            // ИСПРАВЛЕНИЕ 3: Создаем клиента с новым именем
            _grpcClient = new ProfinetStackService.ProfinetStackServiceClient(_grpcChannel);

            var startResponse = await _grpcClient.StartStackAsync(new StartRequest
            {
                InterfaceName = interfaceName,
                StationName = stationName
            });

            if (!startResponse.Success)
            {
                throw new Exception($"Profinet start failed: {startResponse.ErrorMessage}");
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();

            // Открываем двунаправленный стрим
            _ioStream = _grpcClient.StreamIoData(cancellationToken: _cts.Token);

            _ = Task.Run(async () => await ReceiveProfinetDataLoopAsync(), _cts.Token);
        }

        private async Task ReceiveProfinetDataLoopAsync()
        {
            try
            {
                await foreach (var response in _ioStream.ResponseStream.ReadAllAsync(_cts.Token))
                {
                    byte[] data = response.Data.ToByteArray();
                    _profinetMemory[response.Offset] = data;

                    var updatePayload = new
                    {
                        Protocol = "PROFINET",
                        Offset = response.Offset,
                        HexData = BitConverter.ToString(data),
                        IsGoodQuality = response.IsGoodQuality,
                        Time = DateTime.Now.ToString("HH:mm:ss")
                    };

                    await _hubContext.Clients.All.SendAsync("ReceiveProfinetData", updatePayload);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                // Стрим закрыт
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Profinet stream error: {ex.Message}");
            }
        }

        public async Task WriteOutputAsync(int offset, byte[] data)
        {
            if (!IsRunning || _ioStream == null) return;

            var request = new IoWriteRequest
            {
                Offset = offset,
                // ИСПРАВЛЕНИЕ 4: Полный путь к ByteString
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            };

            await _ioStream.RequestStream.WriteAsync(request);
        }

        public async Task StopServerAsync()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            if (_ioStream != null)
            {
                await _ioStream.RequestStream.CompleteAsync();
                _ioStream.Dispose();
            }

            await _grpcClient.StopStackAsync(new StopRequest());

            _grpcChannel?.Dispose();
            IsRunning = false;
        }
    }
}