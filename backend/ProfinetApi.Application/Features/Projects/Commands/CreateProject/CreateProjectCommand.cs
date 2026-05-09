using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json.Serialization;

namespace ProfinetApi.Application.Features.Projects.Commands.CreateProject;

public record CreateProjectCommand([property: JsonPropertyName("name")] string Name) : IRequest<Guid>;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateProjectCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        var project = new Project(request.Name);

        await _repository.AddAsync(project, ct);

        return project.Id;
    }
}
