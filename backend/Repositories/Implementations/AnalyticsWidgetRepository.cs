using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class AnalyticsWidgetRepository(AppDbContext dbContext) : IAnalyticsWidgetRepository
{
    public Task<List<AnalyticsWidget>> ListAsync(CancellationToken cancellationToken)
    {
        return dbContext.AnalyticsWidgets
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<AnalyticsWidget?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.AnalyticsWidgets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task AddAsync(AnalyticsWidget widget, CancellationToken cancellationToken)
    {
        return dbContext.AnalyticsWidgets.AddAsync(widget, cancellationToken).AsTask();
    }

    public void Remove(AnalyticsWidget widget)
    {
        dbContext.AnalyticsWidgets.Remove(widget);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
