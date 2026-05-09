using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Servers.Commands.CreateServer;
using ProfinetApi.Application.Features.Servers.Commands.DiscoverDevices;

namespace ProfinetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfinetServersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfinetServersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProfinetServerCommand command)
    {
        try
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("discovery")]
    public async Task<IActionResult> Discover([FromBody] DiscoverDevicesCommand command)
    {
        try
        {
            // Контроллер просто ждет результата 3.5 секунды
            var result = await _mediator.Send(command);

            // Если ничего не нашли, можно вернуть 200 OK, но с Success = false (и пустым массивом Devices), 
            // чтобы фронтенд красиво показал сообщение "Устройства не найдены"
            return Ok(result);
        }
        catch (TaskCanceledException)
        {
            // Если пользователь (или браузер) отменил запрос до истечения 3.5 секунд
            return StatusCode(499, new { message = "Запрос был отменен пользователем." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Ошибка сканирования: {ex.Message}" });
        }
    }
}