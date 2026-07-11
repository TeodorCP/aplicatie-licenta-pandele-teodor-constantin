using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IRoleService
{
    Task<ServiceResult<List<RoleDto>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ServiceResult<RoleDto>> CreateAsync(ClaimsPrincipal principal, CreateRoleRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<RoleDto>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateRoleRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
