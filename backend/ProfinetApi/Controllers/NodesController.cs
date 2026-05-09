using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Nodes.DeleteNode;

namespace ProfinetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNode(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteNodeCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}