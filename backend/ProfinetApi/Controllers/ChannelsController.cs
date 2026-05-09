using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Channels.Commands.CreateChannel;
using ProfinetApi.Application.Features.Channels.Commands.UpdateChannelConfiguration;

namespace ProfinetApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChannelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChannelsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChannelCommand command)
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

    [HttpPut("{id}/configuration")]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] IecChannelConfigDto configuration)
    {
        try
        {
            var command = new UpdateChannelConfigurationCommand(id, configuration);
            await _mediator.Send(command);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
