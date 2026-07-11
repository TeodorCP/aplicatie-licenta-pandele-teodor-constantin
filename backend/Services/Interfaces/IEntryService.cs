using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IEntryService
{
    Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, Guid moduleId, CreateEntryRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<List<object>>> ListByModuleAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken);

    Task<ServiceResult<object>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateEntryRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<object>> GetVisibilityAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateVisibilityAsync(ClaimsPrincipal principal, Guid id, UpdateEntryVisibilityRequestDto request, CancellationToken cancellationToken);
}
