using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Channels.Commands.UpdateChannelConfiguration;

public record UpdateChannelConfigurationCommand(
    Guid ChannelId,
    IecChannelConfigDto Configuration
) : IRequest;

public class UpdateChannelConfigurationCommandHandler : IRequestHandler<UpdateChannelConfigurationCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateChannelConfigurationCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateChannelConfigurationCommand request, CancellationToken cancellationToken)
    {
        var projects = await _repository.GetAllAsync(cancellationToken);

        Project? targetProject = null;
        IecChannel? targetChannel = null;

        foreach (var project in projects)
        {
            foreach (var server in project.Servers)
            {
                if (server is IecServer iecServer)
                {
                    foreach (var netInterface in iecServer.Interfaces.OfType<IecInterface>())
                    {
                        var foundChannel = netInterface.Channels.FirstOrDefault(c => c.Id == request.ChannelId);
                        if (foundChannel != null)
                        {
                            targetChannel = foundChannel;
                            targetProject = project;
                            break;
                        }
                    }
                }
                if (targetChannel != null) break;
            }
            if (targetChannel != null) break;
        }

        if (targetChannel == null || targetProject == null)
        {
            throw new KeyNotFoundException($"Channel with ID {request.ChannelId} not found.");
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        targetChannel.ConfigurationData = JsonSerializer.Serialize(request.Configuration, options);

        await _repository.UpdateAsync(targetProject, cancellationToken);
    }
}
