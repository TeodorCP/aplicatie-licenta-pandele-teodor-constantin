using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IPermissionManagementService
{
    Task<ServiceResult<object>> GetModulePermissionsAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateModulePermissionsAsync(ClaimsPrincipal principal, Guid moduleId, UpdateModulePermissionsRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> GetFieldPermissionsAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken);

    Task<ServiceResult<object>> UpdateFieldPermissionsAsync(ClaimsPrincipal principal, Guid moduleId, UpdateFieldPermissionsRequestDto request, CancellationToken cancellationToken);
}
