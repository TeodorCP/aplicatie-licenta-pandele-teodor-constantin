using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class DashboardWidgetRepository(AppDbContext dbContext) : IDashboardWidgetRepository
{
    public Task<List<DashboardWidget>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.DashboardWidgets
            .AsNoTracking()
            .OrderBy(x => x.Y ?? int.MaxValue)
            .ThenBy(x => x.X ?? int.MaxValue)
            .ThenBy(x => x.Position)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<DashboardWidget?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.DashboardWidgets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task AddAsync(DashboardWidget widget, CancellationToken cancellationToken)
    {
        return dbContext.DashboardWidgets.AddAsync(widget, cancellationToken).AsTask();
    }

    public void Remove(DashboardWidget widget)
    {
        dbContext.DashboardWidgets.Remove(widget);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
