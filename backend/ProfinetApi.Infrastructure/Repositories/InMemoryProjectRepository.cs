using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Infrastructure.Repositories;

public class InMemoryProjectRepository : IProjectRepository
{
    // Хранилище в памяти
    private readonly List<Project> _projects = new();

    public Task AddAsync(Project project, CancellationToken ct = default)
    {
        _projects.Add(project);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var project = _projects.FirstOrDefault(p => p.Id == id);
        if (project != null)
        {
            _projects.Remove(project);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Project>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult((IEnumerable<Project>)_projects);
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var project = _projects.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(project);
    }

    public Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
