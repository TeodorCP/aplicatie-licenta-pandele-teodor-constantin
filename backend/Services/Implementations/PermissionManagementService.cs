using System.Security.Claims;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class PermissionManagementService(
    IModuleRepository moduleRepository,
    IPermissionRepository permissionRepository,
    PermissionService permissionService) : IPermissionManagementService
{
    public async Task<ServiceResult<object>> GetModulePermissionsAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManagePermissionsAsync(principal, moduleId, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        return await moduleRepository.ExistsAsync(moduleId, cancellationToken)
            ? ServiceResult<object>.Ok(await permissionService.GetEffectiveModulePermissionsAsync(moduleId, cancellationToken))
            : ServiceResult<object>.NotFound("Module not found.");
    }

    public async Task<ServiceResult<object>> UpdateModulePermissionsAsync(ClaimsPrincipal principal, Guid moduleId, UpdateModulePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManagePermissionsAsync(principal, moduleId, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var module = await moduleRepository.GetByIdAsync(moduleId, cancellationToken);
        if (module is null)
        {
            return ServiceResult<object>.NotFound("Module not found.");
        }

        var roles = await permissionRepository.GetRolesByIdAsync(cancellationToken);
        var existing = await permissionRepository.GetMutableModulePermissionsAsync(moduleId, cancellationToken);

        foreach (var item in request.Permissions)
        {
            if (!Guid.TryParse(item.RoleId, out var roleId) || !roles.ContainsKey(roleId))
            {
                return ServiceResult<object>.BadRequest($"Invalid role id '{item.RoleId}'.");
            }

            var current = existing.FirstOrDefault(x => x.RoleId == roleId);
            if (current is null)
            {
                current = new ModulePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    ModuleId = moduleId
                };
                await permissionRepository.AddModulePermissionAsync(current, cancellationToken);
                existing.Add(current);
            }

            current.CanView = item.CanView;
            current.CanCreate = item.CanCreate;
            current.CanEdit = item.CanEdit;
            current.CanDelete = item.CanDelete;
            current.CanManagePermissions = item.CanManagePermissions;
        }

        await permissionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Ok(await permissionService.GetEffectiveModulePermissionsAsync(moduleId, cancellationToken));
    }

    public async Task<ServiceResult<object>> GetFieldPermissionsAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManagePermissionsAsync(principal, moduleId, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        return await moduleRepository.ExistsAsync(moduleId, cancellationToken)
            ? ServiceResult<object>.Ok(await permissionService.GetEffectiveFieldPermissionsAsync(moduleId, cancellationToken))
            : ServiceResult<object>.NotFound("Module not found.");
    }

    public async Task<ServiceResult<object>> UpdateFieldPermissionsAsync(ClaimsPrincipal principal, Guid moduleId, UpdateFieldPermissionsRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManagePermissionsAsync(principal, moduleId, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var module = await moduleRepository.GetByIdAsync(moduleId, cancellationToken);
        if (module is null)
        {
            return ServiceResult<object>.NotFound("Module not found.");
        }

        var validFieldNames = PermissionService.BuildFieldMap(module.Fields).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = await permissionRepository.GetRolesByIdAsync(cancellationToken);
        var existing = await permissionRepository.GetMutableFieldPermissionsAsync(moduleId, cancellationToken);

        foreach (var item in request.Permissions)
        {
            if (!Guid.TryParse(item.RoleId, out var roleId) || !roles.ContainsKey(roleId))
            {
                return ServiceResult<object>.BadRequest($"Invalid role id '{item.RoleId}'.");
            }

            var fieldName = item.FieldName.Trim();
            if (!validFieldNames.Contains(fieldName))
            {
                return ServiceResult<object>.BadRequest($"Invalid field name '{item.FieldName}'.");
            }

            var current = existing.FirstOrDefault(x =>
                x.RoleId == roleId &&
                x.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (current is null)
            {
                current = new FieldPermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    ModuleId = moduleId,
                    FieldName = fieldName
                };
                await permissionRepository.AddFieldPermissionAsync(current, cancellationToken);
                existing.Add(current);
            }

            current.CanView = item.CanView;
            current.CanEdit = item.CanEdit;
        }

        await permissionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Ok(await permissionService.GetEffectiveFieldPermissionsAsync(moduleId, cancellationToken));
    }
}
