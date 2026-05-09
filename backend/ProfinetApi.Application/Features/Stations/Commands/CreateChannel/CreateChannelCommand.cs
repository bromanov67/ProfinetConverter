using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.IEC104;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.Channels.Commands.CreateChannel;

public record CreateChannelCommand(Guid InterfaceId, string Name) : IRequest<Guid>;

public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public CreateChannelCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateChannelCommand request, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);

        IecInterface? targetInterface = null;
        Project? targetProject = null;

        foreach (var proj in projects)
        {
            foreach (var srv in proj.Servers)
            {
                var iface = srv.Interfaces.FirstOrDefault(i => i.Id == request.InterfaceId);

                if (iface is IecInterface iecIface)
                {
                    targetInterface = iecIface;
                    targetProject = proj;
                    break;
                }
            }
            if (targetProject != null) break;
        }

        if (targetProject == null || targetInterface == null)
        {
            throw new KeyNotFoundException("Parent IEC interface not found or invalid interface type.");
        }

        var channel = new IecChannel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Active = true,
            IpAddress = "127.0.0.1",
            Port = 2404
        };

        targetInterface.Channels.Add(channel);

        await _repository.UpdateAsync(targetProject, ct);

        return channel.Id;
    }
}
