using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> ListAsync(CancellationToken cancellationToken);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, Guid? roleIdToExclude, CancellationToken cancellationToken);

    Task AddAsync(Role role, CancellationToken cancellationToken);

    void Remove(Role role);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
