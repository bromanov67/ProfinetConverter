using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Infrastructure.Services;

[ApiController]
[Route("api/[controller]")]
public class ProfinetRuntimeController : ControllerBase
{
    private readonly IProfinetRuntimeService _profinetService;

    public ProfinetRuntimeController(IProfinetRuntimeService profinetService)
    {
        _profinetService = profinetService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromQuery] string interfaceName = "eth0", [FromQuery] string stationName = "plc1")
    {
        await _profinetService.StartServerAsync(interfaceName, stationName);
        return Ok();
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await _profinetService.StopServerAsync();
        return Ok();
    }
}