using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class RoleRepository(AppDbContext dbContext) : IRoleRepository
{
    public Task<List<Role>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Roles.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? roleIdToExclude, CancellationToken cancellationToken)
    {
        return dbContext.Roles.AnyAsync(
            x => x.Name.ToLower() == name.ToLower() && (!roleIdToExclude.HasValue || x.Id != roleIdToExclude.Value),
            cancellationToken);
    }

    public Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        return dbContext.Roles.AddAsync(role, cancellationToken).AsTask();
    }

    public void Remove(Role role)
    {
        dbContext.Roles.Remove(role);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
