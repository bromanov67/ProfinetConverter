using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.IEC104;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.NetworkInterfaces.Commands.CreateNetworkInterface;

public record CreateNetworkInterfaceCommand(Guid ServerId, string Name) : IRequest<object>;

public class CreateNetworkInterfaceCommandHandler : IRequestHandler<CreateNetworkInterfaceCommand, object>
{
    private readonly IProjectRepository _repository;

    public CreateNetworkInterfaceCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> Handle(CreateNetworkInterfaceCommand request, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);
        var project = projects.FirstOrDefault(p => p.Servers.Any(s => s.Id == request.ServerId));

        if (project == null)
            throw new KeyNotFoundException("Parent server not found");

        var server = project.Servers.First(s => s.Id == request.ServerId);
        InterfaceBase iface;
        object resultDto;

        switch (server)
        {
            case ProfinetServer ps:
                var pIface = new ProfinetInterface
                {
                    Id = Guid.NewGuid(),
                    ServerId = request.ServerId,
                    Name = request.Name,
                    Active = true
                };
                ps.AddInterface(pIface);

                resultDto = new NetworkInterfaceDto(
                    pIface.Id, pIface.Name, "interface_profinet", pIface.Active,
                    pIface.Description, new List<StationDto>()
                );
                break;

            case IecServer iserver:
                var iIface = new IecInterface
                {
                    Id = Guid.NewGuid(),
                    ServerId = request.ServerId,
                    Name = request.Name,
                    Active = true
                };
                iserver.AddInterface(iIface);

                resultDto = new IecNetworkInterfaceDto(
                    iIface.Id, iIface.Name, "interface_iec", iIface.Active,
                    iIface.Description, new List<IecChannelDto>()
                );
                break;

            default:
                throw new InvalidOperationException("Unknown server type");
        }

        await _repository.UpdateAsync(project, ct);
        return resultDto;
    }
}
