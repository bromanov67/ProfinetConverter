using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Application.ServiceInterfaces;

namespace ProfinetApi.Api.Controllers
{
    [ApiController]
    [Route("api/runtime")]
    public class RuntimeController : ControllerBase
    {
        private readonly IIec104RuntimeService _runtimeService;
        private readonly ILogger<RuntimeController> _logger;
        public RuntimeController(IIec104RuntimeService runtimeService, ILogger<RuntimeController> logger)
        {
            _runtimeService = runtimeService;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartProcess([FromBody] StartProfinetDto requestDto)
        {
            // 1. Проверка входящих данных
            if (requestDto == null)
            {
                _logger.LogWarning("StartProcess вызван с пустым requestDto.");
                return BadRequest(new { message = "Некорректный запрос: отсутствуют данные конфигурации." });
            }

            Channel channel = null;
            try
            {
                // 2. Подготовка конфигурации для МЭК 104
                var iecConfig = requestDto.Signals?.Select(s => new SignalData
                {
                    IOA = s.IOA,
                    Identifier = s.Name,
                    ByteOffset = s.ByteOffset,
                    BitOffset = s.BitOffset,
                    Type = (s.DataType ?? "").ToLower() == "float32" ? "float" : "bool"
                }).ToList() ?? new List<SignalData>();

                // 3. Запуск C# сервера МЭК 104
                _runtimeService.StartServer(requestDto.IecIpAddress, requestDto.IecPort, iecConfig);

                // 4. Подключение к Python через gRPC
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

                // Вызов скрипта
                var response = await client.StartBruteAsync(request, deadline: DateTime.UtcNow.AddSeconds(10));

                if (response.Success)
                {
                    _logger.LogInformation("Режим исполнения успешно запущен.");
                    return Ok(new { message = "Успешный запуск МЭК 104 и Python" });
                }
                else
                {
                    _logger.LogWarning("Python вернул ошибку при запуске: {Message}", response.Message);
                    _runtimeService.StopServer();
                    return BadRequest(new { message = $"Ошибка запуска Python: {response.Message}" });
                }
            }
            catch (RpcException rpcEx)
            {
                _logger.LogError(rpcEx, "gRPC ошибка при попытке связаться с Python.");
                _runtimeService.StopServer();
                return StatusCode(500, new { message = $"gRPC ошибка связи с Python: {rpcEx.Status.Detail}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка в процессе StartProcess.");
                _runtimeService.StopServer();
                return StatusCode(500, new { message = $"Критическая ошибка сервера: {ex.Message}" });
            }
            finally
            {
                if (channel != null)
                {
                    await channel.ShutdownAsync();
                }
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopRuntime()
        {
            try
            {
                _runtimeService.StopServer();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при остановке сервера МЭК 104.");
            }

            Channel channel = null;
            try
            {
                channel = new Channel("192.168.56.102:5005", ChannelCredentials.Insecure);
                var client = new ProfinetController.ProfinetControllerClient(channel);
                await client.StopBruteAsync(new StopRequest(), deadline: DateTime.UtcNow.AddSeconds(5));
            }
            catch (RpcException rpcEx)
            {
                _logger.LogWarning("Python gRPC недоступен при остановке (возможно, уже выключен): {Detail}", rpcEx.Status.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка при остановке Python.");
            }
            finally
            {
                if (channel != null)
                {
                    await channel.ShutdownAsync();
                }
            }

            return Ok(new { message = "Режим исполнения остановлен" });
        }
    }
}