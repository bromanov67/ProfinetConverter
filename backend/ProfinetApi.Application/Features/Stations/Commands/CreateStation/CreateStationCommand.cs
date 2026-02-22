using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Interfaces;

namespace ProfinetApi.Application.Features.Stations.Commands.CreateStation;

public record CreateStationCommand(Guid InterfaceId, string Name) : IRequest<Guid>;

public class CreateStationCommandHandler : IRequestHandler<CreateStationCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateStationCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateStationCommand request, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);

        // Сложный поиск: Проект -> Сервер -> Интерфейс
        // В реальной БД это было бы проще (SELECT ... FROM Interfaces WHERE Id = ...)
        NetworkInterface? targetInterface = null;
        Project? targetProject = null;

        foreach (var proj in projects)
        {
            foreach (var srv in proj.Servers)
            {
                var iface = srv.Interfaces.FirstOrDefault(i => i.Id == request.InterfaceId);
                if (iface != null)
                {
                    targetInterface = iface;
                    targetProject = proj;
                    break;
                }
            }
            if (targetProject != null) break;
        }

        if (targetProject == null || targetInterface == null)
            throw new KeyNotFoundException("Parent interface not found");

        var station = new Station(request.Name, 0); // Address 0 по умолчанию
        targetInterface.AddStation(station);

        await _repository.UpdateAsync(targetProject, ct);
        return station.Id;
    }
}
