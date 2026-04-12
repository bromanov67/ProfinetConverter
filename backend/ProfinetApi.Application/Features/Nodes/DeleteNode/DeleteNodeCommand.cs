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
        // Ищем проект, в котором есть этот узел.
        // Так как БД нет, перебираем все проекты в памяти (это быстро).
        var allProjects = await _repository.GetAllAsync(ct);

        foreach (var project in allProjects)
        {
            // Предполагаем, что у Project есть метод RemoveNodeRecursive,
            // который возвращает true, если узел был найден и удален.
            bool wasRemoved = project.RemoveNodeRecursive(request.NodeId);

            if (wasRemoved)
            {
                // Если нужно явно обновить состояние (для будущего)
                await _repository.UpdateAsync(project, ct);
                return; // Удалили и вышли
            }
        }

        // Если дошли сюда — узел не найден. Можно кинуть исключение или проигнорировать.
        // throw new KeyNotFoundException($"Node {request.NodeId} not found");
    }
}
