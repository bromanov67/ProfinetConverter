using Grpc.Core; // ВАЖНО: Используем Grpc.Core вместо Grpc.Net.Client!
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Application.Services;

namespace ProfinetApi.Api.Controllers
{
    [ApiController]
    [Route("api/runtime")]
    public class RuntimeController : ControllerBase
    {
        private readonly IIec104RuntimeService _runtimeService;

        public RuntimeController(IIec104RuntimeService runtimeService)
        {
            _runtimeService = runtimeService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartProcess([FromBody] StartProfinetDto requestDto)
        {
            if (requestDto == null)
            {
                Console.WriteLine("\n[C#] ОШИБКА: requestDto пришел как NULL!");
            }
            else
            {
                Console.WriteLine($"\n[C#] Получен POST /start. Интерфейс: {requestDto.InterfaceName}");
                Console.WriteLine($"[C#] Кол-во сигналов в DTO.Signals = {(requestDto.Signals == null ? "NULL" : requestDto.Signals.Count.ToString())}");

                if (requestDto.Signals != null && requestDto.Signals.Count > 0)
                {
                    var firstSignal = requestDto.Signals.First();
                    Console.WriteLine($"[C#] Первый сигнал из DTO: Name={firstSignal.Name}, DataType={firstSignal.DataType}, Byte={firstSignal.ByteOffset}, IOA={firstSignal.IOA}");
                }
            }

            Channel channel = null;
            try
            {
                // 1. ЗАПУСК IEC 104 СЕРВЕРА
                var iecConfig = requestDto?.Signals?.Select(s => new SignalData
                {
                    IOA = s.IOA,
                    Identifier = s.Name,
                    ByteOffset = s.ByteOffset,
                    BitOffset = s.BitOffset,
                    Type = (s.DataType ?? "").ToLower() == "float32" ? "float" : "bool"
                }).ToList() ?? new List<SignalData>();

                Console.WriteLine($"[C#] Передаем {iecConfig.Count} элементов в StartServer()");

                _runtimeService.StartServer(requestDto.IecIpAddress, requestDto.IecPort, iecConfig);

                // 2. ВЫЗОВ PYTHON СКРИПТА
                channel = new Channel("192.168.56.102:5005", ChannelCredentials.Insecure);
                var client = new ProfinetController.ProfinetControllerClient(channel);

                var request = new StartRequest
                {
                    InterfaceName = requestDto.InterfaceName,
                    TargetName = requestDto.StationName,
                    ModuleIdent = requestDto.ModuleIdent,
                    SubmoduleIdent = requestDto.SubmoduleIdent,
                    InputLength = requestDto.InputLength,
                    OutputLength = requestDto.OutputLength
                };

                // Добавляем таймаут (опционально, но полезно)
                var response = await client.StartBruteAsync(request, deadline: DateTime.UtcNow.AddSeconds(10));

                if (response.Success)
                    return Ok(new { message = "Успешный запуск МЭК 104 и Python" });
                else
                    return BadRequest($"Ошибка запуска Python: {response.Message}");
            }
            catch (RpcException rpcEx)
            {
                _runtimeService.StopServer();
                return StatusCode(500, $"gRPC ошибка связи с Python: {rpcEx.Status.Detail}");
            }
            catch (Exception ex)
            {
                _runtimeService.StopServer();
                return StatusCode(500, $"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                // Обязательно закрываем канал после разового вызова
                if (channel != null)
                {
                    await channel.ShutdownAsync();
                }
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopRuntime()
        {
            _runtimeService.StopServer();

            Channel channel = null;
            try
            {
                channel = new Channel("192.168.56.102:5005", ChannelCredentials.Insecure);
                var client = new ProfinetController.ProfinetControllerClient(channel);
                await client.StopBruteAsync(new StopRequest(), deadline: DateTime.UtcNow.AddSeconds(5));
            }
            catch
            {
                // Игнорируем
            }
            finally
            {
                if (channel != null) await channel.ShutdownAsync();
            }

            return Ok(new { message = "Runtime Stopped" });
        }
    }
}