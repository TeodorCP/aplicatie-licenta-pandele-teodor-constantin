using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class EntryRepository(AppDbContext dbContext) : IEntryRepository
{
    public Task<Entry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Entries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<Entry>> ListByModuleAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.Entries
            .Where(x => x.ModuleId == moduleId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Entry entry, CancellationToken cancellationToken)
    {
        return dbContext.Entries.AddAsync(entry, cancellationToken).AsTask();
    }

    public void Remove(Entry entry)
    {
        dbContext.Entries.Remove(entry);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
