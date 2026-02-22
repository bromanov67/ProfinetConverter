using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Servers.Commands.CreateServer;

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
    public async Task<IActionResult> Create([FromBody] CreateServerCommand command)
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
}