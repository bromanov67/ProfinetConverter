using MediatR;
using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Config.Commands.LoadConfiguration;

public class LoadConfigurationCommandHandler : IRequestHandler<LoadConfigurationCommand, List<Project>>
{
    private readonly IProjectRepository _repository;

    public LoadConfigurationCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Project>> Handle(LoadConfigurationCommand request, CancellationToken cancellationToken)
    {
        var targetPath = request.FilePath?.Trim();
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("File path is required.");

        if (!File.Exists(targetPath))
            throw new FileNotFoundException($"Configuration file '{targetPath}' not found.");

        var json = await File.ReadAllTextAsync(targetPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            var emptyProjects = new List<Project>();
            await _repository.ReplaceAllAsync(emptyProjects, cancellationToken);
            return emptyProjects;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var projects = JsonSerializer.Deserialize<List<Project>>(json, options) ?? new List<Project>();

        await _repository.ReplaceAllAsync(projects, cancellationToken);

        return projects;
    }
}