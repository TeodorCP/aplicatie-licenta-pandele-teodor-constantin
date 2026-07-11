using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class VisualizationRepository(AppDbContext dbContext) : IVisualizationRepository
{
    public Task<List<Visualization>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.Visualizations.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<List<Visualization>> ListByModuleAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.Visualizations
            .Where(x => x.ModuleId == moduleId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Visualization?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Visualizations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Dictionary<Guid, string>> GetModuleNamesAsync(CancellationToken cancellationToken)
    {
        return dbContext.Modules.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
    }

    public Task AddAsync(Visualization visualization, CancellationToken cancellationToken)
    {
        return dbContext.Visualizations.AddAsync(visualization, cancellationToken).AsTask();
    }

    public void Remove(Visualization visualization)
    {
        dbContext.Visualizations.Remove(visualization);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
