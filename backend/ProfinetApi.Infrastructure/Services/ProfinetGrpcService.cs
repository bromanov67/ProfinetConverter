using Grpc.Core;
using ProfinetApi.Application.Services;

namespace ProfinetApi.Infrastructure.Services
{
    public class ProfinetGrpcService : ProfinetService.ProfinetServiceBase
    {
        private readonly IIec104RuntimeService _iec104RuntimeService;

        // Внедряем сервис МЭК 104 через конструктор
        public ProfinetGrpcService(IIec104RuntimeService iec104RuntimeService)
        {
            _iec104RuntimeService = iec104RuntimeService;
        }

        public override async Task<PayloadResponse> StreamPayload(IAsyncStreamReader<PayloadRequest> requestStream, ServerCallContext context)
        {
            Console.WriteLine("\n[C# gRPC] Входящий стрим от Python успешно открыт!");

            await foreach (var message in requestStream.ReadAllAsync())
            {
                byte[] rawPayload = message.Payload.ToByteArray();

                // Отрезаем последний байт статуса
                byte[] cleanData = new byte[rawPayload.Length - 1];
                Array.Copy(rawPayload, cleanData, cleanData.Length);

                // Передаем "чистые" байты в движок МЭК 104
                _iec104RuntimeService.UpdateMemoryFromProfinet(cleanData);
            }

            Console.WriteLine("[C# gRPC] Стрим завершен.");
            return new PayloadResponse { Success = true };
        }
    }
}