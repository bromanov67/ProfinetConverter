using MediatR;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.Projects.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto?>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IProjectRepository _repository;

    public GetProjectByIdQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.Id, ct);
        if (project == null) return null;

        // Маппинг
        return new ProjectDto(project.Id, project.Name, "project", project.CreatedAt, new List<object>());
    }
}
