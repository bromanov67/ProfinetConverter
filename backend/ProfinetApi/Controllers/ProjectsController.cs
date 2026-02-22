using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Application.Features.Projects.Commands.CreateProject;
using ProfinetApi.Application.Features.Projects.Queries.GetAllProjects;
using ProfinetApi.Application.Features.Projects.Queries.GetProjectById;

namespace ProfinetApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/projects
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
    {
        var query = new GetAllProjectsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // GET /api/projects/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProject(Guid id)
    {
        var query = new GetProjectByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST /api/projects
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command)
    {
        var id = await _mediator.Send(command);
        // Возвращаем 201 Created с заголовком Location
        return CreatedAtAction(nameof(GetProject), new { id = id }, id);
    }
}