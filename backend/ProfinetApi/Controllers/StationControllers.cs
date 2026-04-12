using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Application.Features.Stations.Commands.CreateStation;
using ProfinetApi.Application.Features.Stations.Commands.ImportGsdml;
using ProfinetApi.Application.Features.Stations.Commands.UpdateStationConfiguration;

namespace ProfinetApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStationCommand command)
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

    [HttpPost("{id}/import-gsdml")]
    public async Task<IActionResult> ImportGsdml(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        using var stream = file.OpenReadStream();

        var command = new ImportGsdmlCommand(id, stream, file.FileName);

        try
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}/configuration")]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] StationConfigDto configuration)
    {
        var command = new UpdateStationConfigurationCommand(id, configuration);
        try
        {
            await _mediator.Send(command);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
