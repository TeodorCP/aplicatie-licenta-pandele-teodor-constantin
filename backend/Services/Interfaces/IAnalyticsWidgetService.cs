using System.Security.Claims;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IAnalyticsWidgetService
{
    Task<ServiceResult<List<AnalyticsWidget>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ServiceResult<AnalyticsWidget>> CreateAsync(ClaimsPrincipal principal, CreateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<AnalyticsWidget>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
