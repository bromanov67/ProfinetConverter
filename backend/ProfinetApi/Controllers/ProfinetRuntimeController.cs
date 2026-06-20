using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.ServiceInterfaces;

namespace ProfinetApi.Api.Controllers 
{
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
        public async Task<IActionResult> Start([FromBody] StartProfinetDto request)
        {
            await _profinetService.StartServerAsync(
                request.InterfaceName,
                request.StationName,
                request.ModuleIdent,
                request.SubmoduleIdent,
                request.InputLength,
                request.OutputLength
            );

            return Ok(new { message = "Profinet Runtime (Python) Started" });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            await _profinetService.StopServerAsync();
            return Ok(new { message = "Profinet Runtime Stopped" });
        }
    }
}