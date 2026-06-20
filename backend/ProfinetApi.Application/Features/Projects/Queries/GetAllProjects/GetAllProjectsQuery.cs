using MediatR;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Projects.Queries.GetAllProjects;

public record GetAllProjectsQuery : IRequest<IEnumerable<object>>;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, IEnumerable<object>>
{
    private readonly IProjectRepository _repository;
    private readonly JsonSerializerOptions _jsonOptions;

    public GetAllProjectsQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<IEnumerable<object>> Handle(GetAllProjectsQuery request, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);

        return projects.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            type = "project",
            createdAt = p.CreatedAt,
            servers = p.Servers.Select(server =>
            {
                if (server is ProfinetServer ps)
                {
                    return (object)new
                    {
                        id = ps.Id,
                        name = ps.Name,
                        type = "server_profinet",
                        active = ps.Active,
                        address = ps.Address,
                        interfaces = ps.Interfaces.OfType<ProfinetInterface>().Select(iface => new
                        {
                            id = iface.Id,
                            name = iface.Name,
                            type = "interface_profinet",
                            active = iface.Active,
                            stations = iface.Stations.Select(s =>
                            {
                                StationConfigDto? parsedConfig = null;
                                if (!string.IsNullOrEmpty(s.ConfigurationData))
                                {
                                    try
                                    {
                                        parsedConfig = JsonSerializer.Deserialize<StationConfigDto>(s.ConfigurationData, _jsonOptions);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Parse error for station {s.Id}: {ex.Message}");
                                    }
                                }

                                return new
                                {
                                    id = s.Id,
                                    name = s.Name,
                                    type = "station",
                                    active = s.Active,
                                    address = s.Address,
                                    description = s.Description ?? "",
                                    configuration = parsedConfig ?? new StationConfigDto
                                    {
                                        Id = s.ConfigIdentifier ?? "",
                                        Manufacturer = s.ConfigManufacturer ?? "",
                                        Model = s.ConfigModel ?? "",
                                        Version = s.ConfigVersion ?? "",
                                        ShortDesignation = "Fast Ethernet",
                                        DeviceDescription = "",
                                        ArticleNo = "",
                                        FirmwareVersion = "",
                                        HardwareVersion = "1",
                                        GsdFile = "",
                                        ProfinetDeviceName = "",
                                        IpAddress = "192.168.0.1",
                                        SubnetMask = "255.255.255.0",
                                        DeviceNumber = 1,
                                        Consistency = "",
                                        Slots = new List<GsdmlSlotDto>(),
                                        Modules = new List<GsdmlModuleDto>()
                                    }
                                };
                            }).ToList()
                        }).ToList()
                    };
                }

                if (server is IecServer iecs)
                {
                    return (object)new
                    {
                        id = iecs.Id,
                        name = iecs.Name,
                        type = "server_iec104",
                        active = iecs.Active,
                        interfaces = iecs.Interfaces.OfType<IecInterface>().Select(iface => new
                        {
                            id = iface.Id,
                            name = iface.Name,
                            type = "interface_iec",
                            active = iface.Active,
                            channels = iface.Channels.Select(ch =>
                            {
                                IecChannelConfigDto? parsedConfig = null;
                                if (!string.IsNullOrEmpty(ch.ConfigurationData))
                                {
                                    try
                                    {
                                        parsedConfig = JsonSerializer.Deserialize<IecChannelConfigDto>(ch.ConfigurationData, _jsonOptions);
                                    }
                                    catch { }
                                }

                                return new
                                {
                                    id = ch.Id,
                                    name = ch.Name,
                                    type = "channel_iec",
                                    active = ch.Active,
                                    ipAddress = ch.IpAddress,
                                    port = ch.Port,
                                    timezone = ch.Timezone,
                                    k = ch.K,
                                    w = ch.W,
                                    t0 = ch.T0,
                                    t1 = ch.T1,
                                    t2 = ch.T2,
                                    t3 = ch.T3,
                                    buffering = ch.Buffering,
                                    bufferTi = ch.BufferTi,
                                    bufferTs = ch.BufferTs,

                                    commands = parsedConfig?.Commands ?? new List<IecCommandDto>(),
                                    signals = parsedConfig?.Signals ?? new List<IecSignalDto>()
                                };
                            }).ToList()
                        }).ToList()
                    };
                }

                return null;
            }).Where(s => s != null).ToList()
        });
    }
}
