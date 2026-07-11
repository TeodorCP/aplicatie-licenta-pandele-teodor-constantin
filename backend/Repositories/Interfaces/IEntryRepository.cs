using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IEntryRepository
{
    Task<Entry?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Entry>> ListByModuleAsync(Guid moduleId, CancellationToken cancellationToken);

    Task AddAsync(Entry entry, CancellationToken cancellationToken);

    void Remove(Entry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
