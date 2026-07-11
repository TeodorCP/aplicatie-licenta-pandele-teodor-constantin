using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class ModuleRepository(AppDbContext dbContext) : IModuleRepository
{
    public Task<List<Module>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.Modules.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Module?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Modules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Modules.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, Guid? moduleIdToExclude, CancellationToken cancellationToken)
    {
        return dbContext.Modules.AnyAsync(
            x => x.Name.ToLower() == name.ToLower() && (!moduleIdToExclude.HasValue || x.Id != moduleIdToExclude.Value),
            cancellationToken);
    }

    public Task AddAsync(Module module, CancellationToken cancellationToken)
    {
        return dbContext.Modules.AddAsync(module, cancellationToken).AsTask();
    }

    public void Remove(Module module)
    {
        dbContext.Modules.Remove(module);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
