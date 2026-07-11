using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IAnalyticsWidgetRepository
{
    Task<List<AnalyticsWidget>> ListAsync(CancellationToken cancellationToken);

    Task<AnalyticsWidget?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(AnalyticsWidget widget, CancellationToken cancellationToken);

    void Remove(AnalyticsWidget widget);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
