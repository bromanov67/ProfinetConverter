using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Stations.Commands.UpdateStationConfiguration;

public record UpdateStationConfigurationCommand(
    Guid StationId,
    StationConfigDto Configuration
) : IRequest;

public class UpdateStationConfigurationCommandHandler : IRequestHandler<UpdateStationConfigurationCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateStationConfigurationCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateStationConfigurationCommand request, CancellationToken cancellationToken)
    {
        var projects = await _repository.GetAllAsync(cancellationToken);

        Project? targetProject = null;
        Station? targetStation = null;

        foreach (var project in projects)
        {
            foreach (var server in project.Servers)
            {
                if (server is not ProfinetServer profinetServer)
                    continue;

                foreach (var netInterface in profinetServer.Interfaces.OfType<ProfinetInterface>())
                {
                    var foundStation = netInterface.Stations.FirstOrDefault(s => s.Id == request.StationId);
                    if (foundStation is null)
                        continue;

                    targetStation = foundStation;
                    targetProject = project;
                    break;
                }

                if (targetStation is not null)
                    break;
            }

            if (targetStation is not null)
                break;
        }

        if (targetStation is null || targetProject is null)
            throw new KeyNotFoundException($"Station with ID {request.StationId} not found.");

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        targetStation.ConfigurationData = JsonSerializer.Serialize(request.Configuration, options);

        await _repository.UpdateAsync(targetProject, cancellationToken);
    }
}