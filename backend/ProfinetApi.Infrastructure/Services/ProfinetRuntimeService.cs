using Grpc.Net.Client;
using ProfinetApi.Application.Interfaces;
using System.Net.Security;

namespace ProfinetApi.Infrastructure.Services
{
    public class ProfinetRuntimeService : IProfinetRuntimeService
    {
        public async Task StartServerAsync(string interfaceName, string stationName, int moduleIdent, int submoduleIdent, int inputLen, int outputLen)
        {
            try
            {
                string pythonVmUrl = "http://192.168.56.102:5005"; // Порт Python-сервера (HTTPS)
                
                var httpHandler = new SocketsHttpHandler
                {
                    // 1. ОТКЛЮЧАЕМ СИСТЕМНЫЕ ПРОКСИ (Антивирусы, VPN, Fiddler)
                    UseProxy = false,

                    // 2. Игнорируем самоподписанный сертификат
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; }
                    }
                };

                var channelOptions = new GrpcChannelOptions
                {
                    HttpHandler = httpHandler
                };

                using var channel = GrpcChannel.ForAddress(pythonVmUrl, channelOptions);
                var client = new ProfinetController.ProfinetControllerClient(channel);

                var request = new StartRequest
                {
                    InterfaceName = interfaceName,
                    TargetName = stationName,
                    ModuleIdent = moduleIdent,
                    SubmoduleIdent = submoduleIdent,
                    InputLength = inputLen,
                    OutputLength = outputLen
                };
                var response = await client.StartBruteAsync(request);

                Console.WriteLine($"[C#] Python ответил: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# ERR] Ошибка связи: {ex.Message}");
            }
        }

        // Заодно метод для остановки
        public async Task StopServerAsync()
        {
            try
            {
                string pythonVmUrl = "http://192.168.56.101:5001";
                using var channel = GrpcChannel.ForAddress(pythonVmUrl);
                var client = new ProfinetController.ProfinetControllerClient(channel);

                Console.WriteLine("[C#] Отправка команды STOP на Python-микросервис...");
                var response = await client.StopBruteAsync(new StopRequest());

                Console.WriteLine($"[C#] Python ответил: {response.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# ERR] Ошибка при остановке: {ex.Message}");
            }
        }
    }
}