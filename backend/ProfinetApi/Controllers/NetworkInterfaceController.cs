using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.NetworkInterfaces.Commands.CreateNetworkInterface;

namespace ProfinetApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkInterfacesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NetworkInterfacesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNetworkInterfaceCommand command)
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
