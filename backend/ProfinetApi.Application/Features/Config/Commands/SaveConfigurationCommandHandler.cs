using MediatR;
using ProfinetApi.Domain.RepoInterfaces;
using System.Text.Json;

namespace ProfinetApi.Application.Features.Config.Commands.SaveConfiguration;

public class SaveConfigurationCommandHandler : IRequestHandler<SaveConfigurationCommand>
{
    private readonly IProjectRepository _repository;

    public SaveConfigurationCommandHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SaveConfigurationCommand request, CancellationToken cancellationToken)
    {
        var projects = await _repository.GetAllAsync(cancellationToken);

        var targetPath = request.FilePath?.Trim();
        if (string.IsNullOrWhiteSpace(targetPath))
            throw new ArgumentException("File path is required.");

        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory path is invalid.");

        Directory.CreateDirectory(directory);

        if (!Path.HasExtension(targetPath))
            targetPath += ".json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(projects, options);
        await File.WriteAllTextAsync(targetPath, json, cancellationToken);
    }
}