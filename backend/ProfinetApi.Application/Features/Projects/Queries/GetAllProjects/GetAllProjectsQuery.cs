using MediatR;
using ProfinetApi.Application.DTOs;
using ProfinetApi.Domain.Interfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Projects.Queries.GetAllProjects;

public record GetAllProjectsQuery : IRequest<IEnumerable<ProjectDto>>;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, IEnumerable<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public GetAllProjectsQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
        // Настраиваем парсинг так, чтобы он не зависел от регистра символов
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<IEnumerable<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);

        // Полный маппинг всего дерева
        return projects.Select(p => new ProjectDto(
            p.Id,
            p.Name,
            "project",
            p.CreatedAt,
            p.Servers.Select(server => new ProfinetServerDto(
                 server.Id,
                 server.Name,
                 "server",
                 true,
                 0,
                 server.Interfaces.Select(iface => new NetworkInterfaceDto(
                      iface.Id,
                      iface.Name,
                      "interface",
                      iface.Active,
                      iface.Stations.Select(s =>
                      {
                          // 1. Десериализуем данные из JSON строки
                          var parsedConfig = !string.IsNullOrEmpty(s.ConfigurationData)
                              ? JsonSerializer.Deserialize<StationConfigDto>(s.ConfigurationData, _jsonOptions)
                              : null;

                          // 2. Извлекаем слоты и модули (или задаем пустые списки)
                          var slots = parsedConfig?.Slots ?? new List<GsdmlSlotDto>();
                          var modules = parsedConfig?.Modules ?? new List<GsdmlModuleDto>();

                          // 3. Формируем конфиг
                          var config = new StationConfigDto(
                          Id: s.ConfigIdentifier ?? "",
                          Manufacturer: s.ConfigManufacturer ?? "",
                          Model: s.ConfigModel ?? "",
                          Version: s.ConfigVersion ?? "",
                          ShortDesignation: parsedConfig?.ShortDesignation ?? "Fast Ethernet",
                          DeviceDescription: parsedConfig?.DeviceDescription ?? "PROFINET IO Device function",
                          ArticleNo: parsedConfig?.ArticleNo ?? "A02B-xxxx-J147",
                          FirmwareVersion: parsedConfig?.FirmwareVersion ?? "",
                          HardwareVersion: parsedConfig?.HardwareVersion ?? "1",
                          GsdFile: parsedConfig?.GsdFile ?? "gsdml.xml",
                          ProfinetDeviceName: parsedConfig?.ProfinetDeviceName ?? "fanuc-cnc",
                          IpAddress: parsedConfig?.IpAddress ?? "192.168.0.1",
                          SubnetMask: parsedConfig?.SubnetMask ?? "255.255.255.0",
                          DeviceNumber: parsedConfig?.DeviceNumber ?? 1,
                          Consistency: parsedConfig?.Consistency ?? "",
                          Slots: slots,
                          Modules: modules
                        );

                          // 4. Возвращаем StationDto
                          return new StationDto(
                              s.Id,
                              s.Name,
                              "station",
                              s.Active,
                              s.Address,
                              s.Description ?? "",
                              config
                          );
                      }).ToList()
                 )).ToList()
            )).ToList()
        ));
    }
}
