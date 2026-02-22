using MediatR;
using System.Text.Json.Serialization;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Interfaces;

namespace ProfinetApi.Application.Features.Servers.Commands.CreateServer;

public record CreateServerCommand(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("name")] string Name
) : IRequest<Guid>;

public class CreateServerCommandHandler : IRequestHandler<CreateServerCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateServerCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateServerCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.ProjectId, ct);
        if (project == null) throw new KeyNotFoundException("Project not found");

        // 1. Создаем Сервер
        var server = new ProfinetServer(request.Name)
        {
            Active = true,
            Address = 1,
            ProtocolVersion = "V0",
            MasterClass = "Class 1"
        };

        // 2. АВТОМАТИЧЕСКИ создаем дефолтный Интерфейс
        var iface = new NetworkInterface("Ethernet Interface")
        {
            Active = true,
        };

        // 3. АВТОМАТИЧЕСКИ создаем дефолтную Станцию
        var station = new Station("Station", 1)
        {
            Active = true,
            Description = "Station 1",
            ConfigIdentifier = "Station1",
            ConfigVersion = "1.0.0"
        };

        // 4. Связываем всё вместе
        iface.AddStation(station);
        server.AddInterface(iface);
        project.AddServer(server);

        await _repository.UpdateAsync(project, ct);

        return server.Id;
    }
}
