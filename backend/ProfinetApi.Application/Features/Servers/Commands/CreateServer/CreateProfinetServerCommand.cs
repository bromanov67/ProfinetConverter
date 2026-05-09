using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json.Serialization;

namespace ProfinetApi.Application.Features.Servers.Commands.CreateServer;

public record CreateProfinetServerCommand(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("name")] string Name
) : IRequest<Guid>;

public class CreateServerCommandHandler : IRequestHandler<CreateProfinetServerCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateServerCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateProfinetServerCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.ProjectId, ct);
        if (project == null) throw new KeyNotFoundException("Project not found");

        var server = new ProfinetServer()
        {
            Id = Guid.NewGuid(),
            Active = true,
            Name = request.Name,
            Address = 1,
            ProtocolVersion = "V0",
            MasterClass = "Class 1"
        };

        var iface = new ProfinetInterface()
        {
            Id = Guid.NewGuid(),
            Name = "Ethernet",
            Active = true
        };

        var station = new Station("Station", 1)
        {
            Active = true,
            Description = "Station 1",
            ConfigIdentifier = "Station1",
            ConfigVersion = "1.0.0"
        };

        iface.Stations.Add(station);
        server.Interfaces.Add(iface);
        project.Servers.Add(server);

        await _repository.UpdateAsync(project, ct);

        return server.Id;
    }
}
