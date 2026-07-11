using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IDashboardWidgetRepository
{
    Task<List<DashboardWidget>> ListAsync(CancellationToken cancellationToken);

    Task<DashboardWidget?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(DashboardWidget widget, CancellationToken cancellationToken);

    void Remove(DashboardWidget widget);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
