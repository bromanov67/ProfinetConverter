using Microsoft.AspNetCore.Mvc;
using ProfinetApi.API.Serialization;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text;
using System.Text.Json;

namespace ProfinetApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IProjectRepository _repository;

    public ConfigController(IProjectRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("open")]
    public async Task<ActionResult<List<Project>>> Open(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty.");

        string json;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
        {
            json = await reader.ReadToEndAsync(cancellationToken);
        }

        var options = CreateJsonOptions();

        List<Project>? projects;
        try
        {
            projects = JsonSerializer.Deserialize<List<Project>>(json, options);
        }
        catch (JsonException ex)
        {
            return BadRequest($"Invalid JSON: {ex.Message}");
        }

        projects ??= new List<Project>();

        await _repository.ReplaceAllAsync(projects, cancellationToken);

        return Ok(projects);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var projects = await _repository.GetAllAsync(cancellationToken);

        var options = CreateJsonOptions();
        var json = JsonSerializer.Serialize(projects, options);
        var bytes = Encoding.UTF8.GetBytes(json);

        return File(bytes, "application/json", "config.json");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        options.Converters.Add(new ServerJsonConverter());
        options.Converters.Add(new NetworkInterfaceJsonConverter());

        return options;
    }
}