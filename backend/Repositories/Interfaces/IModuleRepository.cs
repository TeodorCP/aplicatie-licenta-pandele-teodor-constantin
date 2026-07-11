using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IModuleRepository
{
    Task<List<Module>> ListAsync(CancellationToken cancellationToken);

    Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? moduleIdToExclude, CancellationToken cancellationToken);

    Task AddAsync(Module module, CancellationToken cancellationToken);

    void Remove(Module module);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
