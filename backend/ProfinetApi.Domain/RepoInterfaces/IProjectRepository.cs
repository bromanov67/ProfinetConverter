using ProfinetApi.Domain.Entities;

namespace ProfinetApi.Domain.RepoInterfaces
{
    public interface IProjectRepository
    {
        Task ReplaceAllAsync(List<Project> projects, CancellationToken cancellationToken);
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Project>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Project project, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task UpdateAsync(Project project, CancellationToken ct = default);
    }
}
