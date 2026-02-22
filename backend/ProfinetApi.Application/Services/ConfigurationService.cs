using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.Interfaces;
using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProfinetApi.Application.Services;

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
        if (project == null || station == null)
            throw new KeyNotFoundException($"Station {stationId} not found");

        try
        {
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, ct);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "http://www.profibus.com/GSDML/2003/11/DeviceProfile";

            // --- 1. Словарь текстов ---
            var textDict = new Dictionary<string, string>();
            foreach (var t in doc.Descendants(ns + "Text"))
            {
                var id = t.Attribute("TextId")?.Value;
                var val = t.Attribute("Value")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(val))
                    textDict[id] = val;
            }
            string Resolve(string? tid) =>
                string.IsNullOrEmpty(tid) ? "" : textDict.TryGetValue(tid, out var v) ? v : tid;

            // --- 2. DeviceIdentity ---
            var identity = doc.Descendants(ns + "DeviceIdentity").FirstOrDefault();
            station.ConfigIdentifier = identity?.Attribute("DeviceID")?.Value ?? "";
            station.ConfigManufacturer = identity?.Element(ns + "VendorName")?.Attribute("Value")?.Value ?? "";

            // --- 3. DAP (DeviceAccessPoint) ---
            var dap = doc.Descendants(ns + "DeviceAccessPointItem").FirstOrDefault();
            var family = doc.Descendants(ns + "Family").FirstOrDefault();

            var mi = dap?.Element(ns + "ModuleInfo");
            station.ConfigModel = family?.Attribute("ProductFamily")?.Value
                                 ?? Resolve(mi?.Element(ns + "Name")?.Attribute("TextId")?.Value);
            station.ConfigVersion = mi?.Element(ns + "HardwareRelease")?.Attribute("Value")?.Value
                                 ?? mi?.Element(ns + "SoftwareRelease")?.Attribute("Value")?.Value
                                 ?? "1.0";

            // --- 4. AllowedInSlots: строим словарь moduleId → [slotNums] ---
            // AllowedInSlots может быть "3 6" (пробел), "1" или "2..5" — обрабатываем всё
            var allowedMap = new Dictionary<string, List<int>>();
            foreach (var modRef in doc.Descendants(ns + "UseableModules")
                                       .FirstOrDefault()
                                       ?.Elements(ns + "ModuleItemRef") ?? Enumerable.Empty<XElement>())
            {
                var target = modRef.Attribute("ModuleItemTarget")?.Value;
                var slotsRaw = modRef.Attribute("AllowedInSlots")?.Value ?? "";
                var slotNums = ParseSlotNumbers(slotsRaw);
                if (target != null)
                    allowedMap[target] = slotNums;
            }

            // --- 5. Каталог модулей с AllowedInSlots ---
            var modulesList = new List<object>();
            foreach (var mod in doc.Descendants(ns + "ModuleItem"))
            {
                var modId = mod.Attribute("ID")?.Value ?? "";
                var nameTextId = mod.Element(ns + "ModuleInfo")?.Element(ns + "Name")?.Attribute("TextId")?.Value;
                var infoTextId = mod.Element(ns + "ModuleInfo")?.Element(ns + "InfoText")?.Attribute("TextId")?.Value;
                var allowed = allowedMap.TryGetValue(modId, out var sl) ? sl : new List<int>();

                modulesList.Add(new
                {
                    id = modId,
                    name = Resolve(nameTextId) ?? modId,
                    info = Resolve(infoTextId),
                    allowedInSlots = allowed
                });
            }

            // --- 6. Слоты с метками (label = категория из первого подходящего модуля) ---
            var slotLabels = new Dictionary<int, string>();
            foreach (var kv in allowedMap)
            {
                var mod = doc.Descendants(ns + "ModuleItem")
                             .FirstOrDefault(m => m.Attribute("ID")?.Value == kv.Key);
                if (mod == null) continue;
                var nameId = mod.Element(ns + "ModuleInfo")?.Element(ns + "Name")?.Attribute("TextId")?.Value;
                var name = Resolve(nameId) ?? kv.Key;
                var label = StripByteCount(name); // "val. effett. 4 Byte IN" → "val. effett."
                foreach (var slotNum in kv.Value)
                    slotLabels.TryAdd(slotNum, label);
            }

            var slotsList = new List<object>();
            var physicalSlots = dap?.Attribute("PhysicalSlots")?.Value ?? "0..0";
            var parts = physicalSlots.Split("..");
            int maxSlot = parts.Length == 2 && int.TryParse(parts[1], out var mx) ? mx : 0;
            for (int i = 1; i <= maxSlot; i++)
            {
                slotsList.Add(new
                {
                    number = i,
                    label = slotLabels.TryGetValue(i, out var lbl) ? lbl : $"Slot {i}",
                    module = (object?)null
                });
            }

            // --- 7. Дополнительные поля из DAP ---
            var shortDesig = Resolve(mi?.Element(ns + "Name")?.Attribute("TextId")?.Value);
            var deviceDescr = Resolve(mi?.Element(ns + "InfoText")?.Attribute("TextId")?.Value);
            var articleNo = mi?.Element(ns + "OrderNumber")?.Attribute("Value")?.Value ?? "";
            var firmwareVer = mi?.Element(ns + "SoftwareRelease")?.Attribute("Value")?.Value ?? "";
            var hardwareVer = mi?.Element(ns + "HardwareRelease")?.Attribute("Value")?.Value ?? "";
            var profinetName = dap?.Attribute("DNS_CompatibleName")?.Value ?? "";

            // --- 8. Сериализация в JSON ---
            station.ConfigurationData = JsonSerializer.Serialize(new
            {
                shortDesignation = shortDesig,
                deviceDescription = deviceDescr,
                articleNo,
                firmwareVersion = firmwareVer,
                hardwareVersion = hardwareVer,
                gsdFile = fileName,
                profinetDeviceName = profinetName,
                ipAddress = "",
                subnetMask = "255.255.255.0",
                deviceNumber = 1,
                consistency = "",
                slots = slotsList,
                modules = modulesList
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            station.Description = $"GSDML imported: {DateTime.Now:yyyy-MM-dd HH:mm} | {modulesList.Count} modules";
            await _repository.UpdateAsync(project, ct);
        }
        catch (Exception ex)
        {
            throw new Exception($"GSDML processing error: {ex.Message}", ex);
        }
    }

    // Разбор строки слотов: "3 6", "1", "2..5" → List<int>
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

    // "val. effett. 4 Byte IN" → "val. effett."
    private static string StripByteCount(string name)
    {
        var m = Regex.Match(name, @"^(.+?)\s+(?:not used|\d+\s+Byte)");
        return m.Success ? m.Groups[1].Value.Trim() : name;
    }

    private async Task<(Project?, Station?)> FindProjectAndStationAsync(Guid stationId, CancellationToken ct)
    {
        var projects = await _repository.GetAllAsync(ct);
        foreach (var proj in projects)
            foreach (var server in proj.Servers)
                foreach (var iface in server.Interfaces)
                {
                    var station = iface.Stations.FirstOrDefault(s => s.Id == stationId);
                    if (station != null) return (proj, station);
                }
        return (null, null);
    }
}
