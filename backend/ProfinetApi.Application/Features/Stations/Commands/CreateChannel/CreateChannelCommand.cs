using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.IEC104;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.Channels.Commands.CreateChannel;

// Команда принимает ID интерфейса и имя канала
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

        // Ищем проект и нужный интерфейс
        foreach (var proj in projects)
        {
            foreach (var srv in proj.Servers)
            {
                // Ищем интерфейс по Id
                var iface = srv.Interfaces.FirstOrDefault(i => i.Id == request.InterfaceId);

                // Если нашли, проверяем, что это именно IecInterface (МЭК 104)
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

        // Создаем канал с дефолтными настройками
        var channel = new IecChannel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Active = true,
            IpAddress = "127.0.0.1",
            Port = 2404
        };

        // Добавляем канал в список интерфейса
        targetInterface.Channels.Add(channel);

        // Сохраняем проект
        await _repository.UpdateAsync(targetProject, ct);

        return channel.Id;
    }
}
