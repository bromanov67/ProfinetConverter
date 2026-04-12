using MediatR;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Domain.RepoInterfaces;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet; // Обязательно добавить этот using для ProfinetServer и ProfinetInterface
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

        // 1. Ищем станцию во всех проектах, серверах и интерфейсах
        foreach (var project in projects)
        {
            foreach (var server in project.Servers)
            {
                // Приводим к ProfinetServer, так как IEC серверы не имеют станций
                if (server is ProfinetServer profinetServer)
                {
                    // Приводим интерфейсы к ProfinetInterface, так как у InterfaceBase нет Stations
                    foreach (var netInterface in profinetServer.Interfaces.OfType<ProfinetInterface>())
                    {
                        var foundStation = netInterface.Stations.FirstOrDefault(s => s.Id == request.StationId);
                        if (foundStation != null)
                        {
                            targetStation = foundStation;
                            targetProject = project; // Сразу запоминаем проект
                            break;
                        }
                    }
                }
                if (targetStation != null) break;
            }
            if (targetStation != null) break;
        }

        // 2. Если станция не найдена, бросаем исключение
        if (targetStation == null || targetProject == null)
        {
            throw new KeyNotFoundException($"Station with ID {request.StationId} not found.");
        }

        // 3. Обновляем конфигурацию
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        targetStation.ConfigurationData = JsonSerializer.Serialize(request.Configuration, options);

        // 4. Сохраняем изменения (вызываем UpdateAsync для найденного проекта)
        await _repository.UpdateAsync(targetProject, cancellationToken);
    }
}
