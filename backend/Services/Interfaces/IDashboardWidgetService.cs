using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IDashboardWidgetService
{
    Task<ServiceResult<List<object>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, CreateDashboardWidgetRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateDashboardWidgetRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
