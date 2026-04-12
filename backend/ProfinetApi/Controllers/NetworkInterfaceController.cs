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
            var createdInterfaceDto = await _mediator.Send(command);
            return Ok(createdInterfaceDto); 
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

}
