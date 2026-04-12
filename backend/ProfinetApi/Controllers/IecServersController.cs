using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Servers.Commands;

namespace ProfinetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IecServersController : ControllerBase
{
    private readonly IMediator _mediator;

    public IecServersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIecServerCommand command)
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
