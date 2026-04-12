using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Application.Features.Stations.Commands.CreateStation;

public record CreateStationCommand(Guid InterfaceId, string Name) : IRequest<Guid>;

public class CreateStationCommandHandler : IRequestHandler<CreateStationCommand, Guid>
{
	private readonly IProjectRepository _repository;

	public CreateStationCommandHandler(IProjectRepository repository)
	{
		_repository = repository;
	}

	public async Task<Guid> Handle(CreateStationCommand request, CancellationToken ct)
	{
		var projects = await _repository.GetAllAsync(ct);

		ProfinetInterface? targetInterface = null;
		Project? targetProject = null;

		// Ищем проект и нужный интерфейс
		foreach (var proj in projects)
		{
			foreach (var srv in proj.Servers)
			{
				// Ищем интерфейс по Id
				var iface = srv.Interfaces.FirstOrDefault(i => i.Id == request.InterfaceId);

				// Если нашли, проверяем, что это именно Profinet интерфейс
				if (iface is ProfinetInterface profinetIface)
				{
					targetInterface = profinetIface;
					targetProject = proj;
					break;
				}
			}
			if (targetProject != null) break;
		}

		if (targetProject == null || targetInterface == null)
		{
			throw new KeyNotFoundException("Parent PROFINET interface not found or invalid interface type.");
		}

		// Создаем станцию
		var station = new Station("Station", 0)
		{
			Name = request.Name,
			Active = true,
			ConfigurationData = null
		};

		// Добавляем станцию в список интерфейса
		targetInterface.Stations.Add(station);

		// Сохраняем проект
		await _repository.UpdateAsync(targetProject, ct);

		return station.Id;
	}
}
