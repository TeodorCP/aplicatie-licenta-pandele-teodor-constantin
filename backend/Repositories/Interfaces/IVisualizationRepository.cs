using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IVisualizationRepository
{
    Task<List<Visualization>> ListAsync(CancellationToken cancellationToken);

    Task<List<Visualization>> ListByModuleAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<Visualization?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Dictionary<Guid, string>> GetModuleNamesAsync(CancellationToken cancellationToken);

    Task AddAsync(Visualization visualization, CancellationToken cancellationToken);

    void Remove(Visualization visualization);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
