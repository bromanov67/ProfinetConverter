using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Interfaces;

namespace ProfinetApi.Application.Features.NetworkInterfaces.Commands.CreateNetworkInterface;

public record CreateNetworkInterfaceCommand(Guid ServerId, string Name) : IRequest<Guid>;

public class CreateNetworkInterfaceCommandHandler : IRequestHandler<CreateNetworkInterfaceCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateNetworkInterfaceCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateNetworkInterfaceCommand request, CancellationToken ct)
    {
        // Ищем проект, содержащий этот сервер
        var projects = await _repository.GetAllAsync(ct);
        var project = projects.FirstOrDefault(p => p.Servers.Any(s => s.Id == request.ServerId));

        if (project == null) throw new KeyNotFoundException("Parent server not found");

        var server = project.Servers.First(s => s.Id == request.ServerId);
        var iface = new NetworkInterface(request.Name);
        server.AddInterface(iface);

        await _repository.UpdateAsync(project, ct);
        return iface.Id;
    }
}
