using ProfinetApi.Domain.Entities;
using ProfinetApi.Domain.RepoInterfaces;

namespace ProfinetApi.Infrastructure.Repositories;

public class InMemoryProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

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
    public async Task ReplaceAllAsync(List<Project> projects, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _projects.Clear();
            _projects.AddRange(projects);
        }
        finally
        {
            _lock.Release();
        }
    }
}
