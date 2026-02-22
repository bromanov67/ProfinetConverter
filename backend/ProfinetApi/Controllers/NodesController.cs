using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.Features.Nodes.DeleteNode;

namespace ProfinetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly IMediator _mediator; // Используем Mediator вместо Service

    public NodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // DELETE /api/nodes/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNode(Guid id)
    {
        try
        {
            // Отправляем команду на удаление через Mediator
            await _mediator.Send(new DeleteNodeCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}