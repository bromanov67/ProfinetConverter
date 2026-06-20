using MediatR;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json.Serialization;

namespace ProfinetApi.Application.Features.Servers.Commands;

public record CreateIecServerCommand(
    [property: JsonPropertyName("projectId")] Guid ProjectId,
    [property: JsonPropertyName("name")] string Name
) : IRequest<Guid>;

public class CreateIecServerCommandHandler : IRequestHandler<CreateIecServerCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateIecServerCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateIecServerCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.ProjectId, ct)
            ?? throw new KeyNotFoundException("Project not found");

        var server = new IecServer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Active = true
        };

        var iface = new IecInterface
        {
            Id = Guid.NewGuid(),
            Name = "Ethernet",
            Active = true
        };

        var channel = new IecChannel
        {
            Id = Guid.NewGuid(),
            Name = "Канал IEC104",
            Active = true,
            IpAddress = "0.0.0.0",
            Port = 2404
        };

        iface.Channels.Add(channel);
        server.AddInterface(iface);

        project.Servers.Add(server);
        await _repository.UpdateAsync(project, ct);

        return server.Id;
    }
}
