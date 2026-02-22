using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Interfaces;
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
        // 1. Создаем сущность
        var project = new Project(request.Name);

        // 2. Сохраняем (в память)
        await _repository.AddAsync(project, ct);

        // SaveChanges не нужен, так как List обновляется мгновенно

        return project.Id;
    }
}
