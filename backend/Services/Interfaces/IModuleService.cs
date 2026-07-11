using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IModuleService
{
    Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, CreateModuleRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<List<object>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ServiceResult<object>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateModuleRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
