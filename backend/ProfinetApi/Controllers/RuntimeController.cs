using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Services;
using ProfinetApi.Infrastructure.Services;

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
    public IActionResult StartRuntime()
    {
        // Здесь вы можете вытащить конфиг каналов из БД
        _runtimeService.StartServer("127.0.0.1", 2404);
        return Ok(new { message = "IEC 104 Server Started" });
    }

    [HttpPost("stop")]
    public IActionResult StopRuntime()
    {
        _runtimeService.StopServer();
        return Ok(new { message = "IEC 104 Server Stopped" });
    }
}