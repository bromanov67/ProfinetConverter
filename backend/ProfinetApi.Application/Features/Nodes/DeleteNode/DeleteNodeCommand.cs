using MediatR;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.Nodes.DeleteNode;

public record DeleteNodeCommand(Guid NodeId) : IRequest;

public class DeleteNodeCommandHandler : IRequestHandler<DeleteNodeCommand>
{
    private readonly IProjectRepository _repository;

    public DeleteNodeCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteNodeCommand request, CancellationToken ct)
    {
        var allProjects = await _repository.GetAllAsync(ct);

        foreach (var project in allProjects)
        {
            bool wasRemoved = project.RemoveNodeRecursive(request.NodeId);

            if (wasRemoved)
            {
                await _repository.UpdateAsync(project, ct);
                return;
            }
        }
    }
}
