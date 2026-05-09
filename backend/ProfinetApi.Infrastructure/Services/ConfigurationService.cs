using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Entities.Profinet;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ProfinetApi.Application.ServiceInterfaces;

public class ConfigurationService : IConfigurationService
{
    private readonly IProjectRepository _repository;

    public ConfigurationService(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task ImportGsdmlAsync(Guid stationId, Stream fileStream, string fileName, CancellationToken ct)
    {
        var (project, station) = await FindProjectAndStationAsync(stationId, ct);
        if (project == null || station == null) throw new KeyNotFoundException($"Station {stationId} not found");

        try
        {
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, ct);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "http://www.profibus.com/GSDML/2003/11/DeviceProfile";

            var textDict = new Dictionary<string, string>();
            foreach (var t in doc.Descendants(ns + "Text"))
            {
                var id = t.Attribute("TextId")?.Value;
                var val = t.Attribute("Value")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(val)) textDict[id] = val;
            }
            string Resolve(string? tid) => string.IsNullOrEmpty(tid) ? "" : textDict.TryGetValue(tid, out var v) ? v : tid;

            var dap = doc.Descendants(ns + "DeviceAccessPointItem").FirstOrDefault();
            var mi = dap?.Element(ns + "ModuleInfo");

            int GetIoLength(XElement element, string direction)
            {
                int totalLength = 0;

                var dataItems = element.Descendants(ns + "IOData")
                                       .Elements(ns + direction)
                                       .Elements(ns + "DataItem");

                foreach (var item in dataItems)
                {
                    if (int.TryParse(item.Attribute("Length")?.Value, out var len))
                    {
                        totalLength += len;
                        continue;
                    }

                    var dataType = item.Attribute("DataType")?.Value;
                    if (!string.IsNullOrEmpty(dataType))
                    {
                        var match = Regex.Match(dataType, @"\d+$");
                        if (match.Success && int.TryParse(match.Value, out var bits))
                        {
                            totalLength += Math.Max(1, bits / 8);
                        }
                        else if (dataType.Equals("Bit", StringComparison.OrdinalIgnoreCase))
                        {
                            totalLength += 1;
                        }
                    }
                }

                return totalLength;
            }

            var allowedMap = new Dictionary<string, List<int>>();
            var useableModules = doc.Descendants(ns + "UseableModules").FirstOrDefault();
            foreach (var modRef in useableModules?.Elements(ns + "ModuleItemRef") ?? Enumerable.Empty<XElement>())
            {
                var target = modRef.Attribute("ModuleItemTarget")?.Value;
                if (target != null) allowedMap[target] = ParseSlotNumbers(modRef.Attribute("AllowedInSlots")?.Value ?? "");
            }

            var catalogModules = new List<object>();
            foreach (var mod in doc.Descendants(ns + "ModuleItem"))
            {
                var modId = mod.Attribute("ID")?.Value ?? "";
                var nId = mod.Element(ns + "ModuleInfo")?.Element(ns + "Name")?.Attribute("TextId")?.Value;

                catalogModules.Add(new
                {
                    id = modId,
                    name = Resolve(nId) ?? modId,
                    allowedInSlots = allowedMap.TryGetValue(modId, out var sl) ? sl : new List<int>(),
                    inputLength = GetIoLength(mod, "Input"),
                    outputLength = GetIoLength(mod, "Output")
                });
            }

            var slotsList = new List<object>();

            bool isCompactPlc = !catalogModules.Any();

            if (isCompactPlc)
            {
                var virtualSubmodules = doc.Descendants(ns + "VirtualSubmoduleItem").ToList();

                var headName = Resolve(mi?.Element(ns + "Name")?.Attribute("TextId")?.Value) ?? "Built-in Base I/O";
                slotsList.Add(new
                {
                    number = 1,
                    label = headName,
                    module = new
                    {
                        id = "builtin_io",
                        name = headName,
                        isBuiltIn = true,
                        inputLength = 0,
                        outputLength = 0
                    }
                });

                int slotCounter = 2;
                foreach (var vsm in virtualSubmodules)
                {
                    var vsmId = vsm.Attribute("ID")?.Value ?? "";
                    var mInfo = vsm.Element(ns + "ModuleInfo");
                    var name = Resolve(mInfo?.Element(ns + "Name")?.Attribute("TextId")?.Value) ?? vsmId;
                    var fixedSubslot = vsm.Attribute("FixedInSubslots")?.Value ?? "";

                    slotsList.Add(new
                    {
                        number = slotCounter,
                        label = $"Channel {fixedSubslot}",
                        module = new
                        {
                            id = vsmId,
                            name = name,
                            isBuiltIn = true,
                            inputLength = GetIoLength(vsm, "Input"),
                            outputLength = GetIoLength(vsm, "Output")
                        }
                    });
                    slotCounter++;
                }
            }
            else
            {
                var physicalSlotsRaw = dap?.Attribute("PhysicalSlots")?.Value ?? "0..0";
                var parts = physicalSlotsRaw.Split("..");

                int maxSlot = parts.Length == 2 && int.TryParse(parts[1], out var mx) ? mx :
                             (int.TryParse(physicalSlotsRaw, out var single) ? single : 0);

                for (int i = 1; i <= maxSlot; i++)
                {
                    slotsList.Add(new
                    {
                        number = i,
                        label = $"Slot {i}",
                        module = (object?)null
                    });
                }
            }

            station.ConfigurationData = JsonSerializer.Serialize(new
            {
                shortDesignation = Resolve(mi?.Element(ns + "Name")?.Attribute("TextId")?.Value),
                deviceDescription = Resolve(mi?.Element(ns + "InfoText")?.Attribute("TextId")?.Value),
                articleNo = mi?.Element(ns + "OrderNumber")?.Attribute("Value")?.Value ?? "",
                firmwareVersion = mi?.Element(ns + "SoftwareRelease")?.Attribute("Value")?.Value ?? "",
                hardwareVersion = mi?.Element(ns + "HardwareRelease")?.Attribute("Value")?.Value ?? "",
                gsdFile = fileName,
                profinetDeviceName = dap?.Attribute("DNS_CompatibleName")?.Value ?? "",
                ipAddress = "",
                subnetMask = "255.255.255.0",
                deviceNumber = 1,
                consistency = "",
                slots = slotsList,
                modules = catalogModules
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            station.Description = $"GSDML imported: {DateTime.Now:yyyy-MM-dd HH:mm}";
            await _repository.UpdateAsync(project, ct);
        }
        catch (Exception ex) { throw new Exception($"GSDML processing error: {ex.Message}", ex); }
    }

    private static List<int> ParseSlotNumbers(string raw)
    {
        var result = new List<int>();
        foreach (var token in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Contains(".."))
            {
                var range = token.Split("..");
                if (int.TryParse(range[0], out var from) && int.TryParse(range[1], out var to))
                    for (int i = from; i <= to; i++) result.Add(i);
            }
            else if (int.TryParse(token, out var num))
                result.Add(num);
        }
        return result;
    }

    private async Task<(Project?, Station?)> FindProjectAndStationAsync(Guid stationId, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);

        foreach (var proj in projects)
        {
            foreach (var server in proj.Servers)
            {
                if (server is ProfinetServer profinetServer)
                {
                    foreach (var iface in profinetServer.Interfaces.OfType<ProfinetInterface>())
                    {
                        var station = iface.Stations.FirstOrDefault(s => s.Id == stationId);
                        if (station != null)
                        {
                            return (proj, station);
                        }
                    }
                }
            }
        }

        return (null, null);
    }
}