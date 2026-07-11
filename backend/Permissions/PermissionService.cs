using System.Security.Claims;
using System.Text.Json;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Permissions;

public enum ModuleAction
{
    View,
    Create,
    Edit,
    Delete,
    ManagePermissions
}

public sealed class UserContext
{
    public required User User { get; init; }

    public required Role Role { get; init; }

    public bool IsOwnerOrAdmin =>
        Role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
        Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}

public sealed class ModuleAuthorizationContext
{
    public required UserContext UserContext { get; init; }

    public required Module Module { get; init; }

    public required ModulePermission ModulePermission { get; init; }

    public required Dictionary<string, FieldPermission> FieldPermissions { get; init; }

    public bool CanViewField(string fieldName)
    {
        if (FieldPermissions.TryGetValue(fieldName, out var permission))
        {
            return permission.CanView;
        }

        return ModulePermission.CanView;
    }

    public bool CanEditField(string fieldName)
    {
        if (FieldPermissions.TryGetValue(fieldName, out var permission))
        {
            return permission.CanEdit;
        }

        return ModulePermission.CanEdit;
    }
}

public sealed class EntryAccessContext
{
    public required ModuleAuthorizationContext ModuleAuthorization { get; init; }

    public required Entry Entry { get; init; }

    public required bool CanViewEntry { get; init; }

    public required bool CanEditEntry { get; init; }

    public bool CanManageVisibility =>
        ModuleAuthorization.UserContext.IsOwnerOrAdmin ||
        Entry.CreatedByUserId == ModuleAuthorization.UserContext.User.Id;
}

public class PermissionService(
    IPermissionRepository permissionRepository,
    IModuleRepository moduleRepository)
{
    public async Task<UserContext?> GetCurrentUserContextAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
            principal.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var user = await permissionRepository.GetActiveUserWithRoleAsync(userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new UserContext
        {
            User = user,
            Role = user.Role
        };
    }

    public async Task<ModuleAuthorizationContext?> AuthorizeModuleAsync(
        ClaimsPrincipal principal,
        Guid moduleId,
        ModuleAction action,
        CancellationToken cancellationToken = default)
    {
        var userContext = await GetCurrentUserContextAsync(principal, cancellationToken);
        if (userContext is null)
        {
            return null;
        }

        var module = await permissionRepository.GetModuleAsync(moduleId, cancellationToken);

        if (module is null)
        {
            return null;
        }

        var modulePermission = await permissionRepository.GetModulePermissionAsync(userContext.Role.Id, moduleId, cancellationToken);

        if (modulePermission is null || !IsAllowed(modulePermission, action))
        {
            return null;
        }

        var fieldPermissions = await permissionRepository.GetFieldPermissionsAsync(userContext.Role.Id, moduleId, cancellationToken);

        return new ModuleAuthorizationContext
        {
            UserContext = userContext,
            Module = module,
            ModulePermission = modulePermission,
            FieldPermissions = fieldPermissions.ToDictionary(x => x.FieldName, StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<EntryAccessContext?> AuthorizeEntryAsync(
        ClaimsPrincipal principal,
        Entry entry,
        ModuleAction action,
        CancellationToken cancellationToken = default)
    {
        var moduleAuthorization = await AuthorizeModuleAsync(principal, entry.ModuleId, action, cancellationToken);
        if (moduleAuthorization is null)
        {
            return null;
        }

        var canView = await CanAccessEntryByVisibilityAsync(moduleAuthorization, entry, allowEdit: false, cancellationToken);
        var canEdit = await CanAccessEntryByVisibilityAsync(moduleAuthorization, entry, allowEdit: true, cancellationToken);

        var isAllowed = action switch
        {
            ModuleAction.View => canView,
            ModuleAction.Create => true,
            ModuleAction.Edit => canEdit,
            ModuleAction.Delete => canEdit,
            _ => false
        };

        if (!isAllowed)
        {
            return null;
        }

        return new EntryAccessContext
        {
            ModuleAuthorization = moduleAuthorization,
            Entry = entry,
            CanViewEntry = canView,
            CanEditEntry = canEdit
        };
    }

    public async Task<List<Entry>> FilterEntriesByVisibilityAsync(
        ModuleAuthorizationContext moduleAuthorization,
        List<Entry> entries,
        CancellationToken cancellationToken = default)
    {
        var entryIds = entries.Select(x => x.Id).ToList();
        var permissions = await permissionRepository.GetEntryVisibilityPermissionsForEntriesAsync(
            entryIds,
            moduleAuthorization.UserContext.Role.Id,
            cancellationToken);
        var permissionLookup = permissions.ToDictionary(x => x.EntryId);

        return entries.Where(entry =>
        {
            if (moduleAuthorization.UserContext.IsOwnerOrAdmin ||
                entry.CreatedByUserId == moduleAuthorization.UserContext.User.Id)
            {
                return true;
            }

            return permissionLookup.TryGetValue(entry.Id, out var permission) && permission.CanView;
        }).ToList();
    }

    public async Task<List<EntryVisibilityPermissionDto>> GetEntryVisibilityPermissionsAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionRepository.GetEntryVisibilityPermissionsAsync(entryId, cancellationToken);
        return permissions
            .Select(x => new EntryVisibilityPermissionDto
            {
                Id = x.Id.ToString(),
                EntryId = x.EntryId.ToString(),
                OwnerUserId = x.OwnerUserId.ToString(),
                RoleId = x.RoleId.ToString(),
                RoleName = x.Role.Name,
                CanView = x.CanView,
                CanEdit = x.CanEdit
            })
            .ToList();
    }

    public async Task EnsureEntryVisibilityDefaultsAsync(
        Entry entry,
        IEnumerable<EntryVisibilityPermissionUpdateDto> requestedPermissions,
        CancellationToken cancellationToken = default)
    {
        var roles = await permissionRepository.ListRolesAsync(cancellationToken);
        var requestedLookup = requestedPermissions
            .Where(x => Guid.TryParse(x.RoleId, out _))
            .ToDictionary(x => Guid.Parse(x.RoleId), x => x);

        foreach (var role in roles)
        {
            var permission = new EntryVisibilityPermission
            {
                Id = Guid.NewGuid(),
                EntryId = entry.Id,
                OwnerUserId = entry.CreatedByUserId,
                RoleId = role.Id,
                CanView = false,
                CanEdit = false
            };

            if (role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                permission.CanView = true;
                permission.CanEdit = true;
            }
            else if (requestedLookup.TryGetValue(role.Id, out var requested))
            {
                permission.CanView = requested.CanView;
                permission.CanEdit = requested.CanView && requested.CanEdit;
            }

            await permissionRepository.AddEntryVisibilityPermissionAsync(permission, cancellationToken);
        }
    }

    public async Task ReplaceEntryVisibilityPermissionsAsync(
        Entry entry,
        IEnumerable<EntryVisibilityPermissionUpdateDto> requestedPermissions,
        CancellationToken cancellationToken = default)
    {
        var existing = await permissionRepository.GetEntryVisibilityPermissionsAsync(entry.Id, cancellationToken);

        permissionRepository.RemoveEntryVisibilityPermissions(existing);
        await permissionRepository.SaveChangesAsync(cancellationToken);

        await EnsureEntryVisibilityDefaultsAsync(entry, requestedPermissions, cancellationToken);
        await permissionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanManageRolesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userContext = await GetCurrentUserContextAsync(principal, cancellationToken);
        return userContext?.IsOwnerOrAdmin == true;
    }

    public async Task<bool> CanManagePermissionsAsync(
        ClaimsPrincipal principal,
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var context = await AuthorizeModuleAsync(principal, moduleId, ModuleAction.ManagePermissions, cancellationToken);
        return context is not null;
    }

    public async Task<List<Module>> GetViewableModulesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userContext = await GetCurrentUserContextAsync(principal, cancellationToken);
        if (userContext is null)
        {
            return [];
        }

        var moduleIds = await permissionRepository.GetViewableModuleIdsAsync(userContext.Role.Id, cancellationToken);
        var moduleIdSet = moduleIds.ToHashSet();

        return (await moduleRepository.ListAsync(cancellationToken))
            .Where(x => moduleIdSet.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    public async Task<List<ModulePermissionDto>> GetEffectiveModulePermissionsAsync(
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionRepository.ListModulePermissionsAsync(moduleId, cancellationToken);
        return permissions
            .Select(x => new ModulePermissionDto
            {
                Id = x.Id.ToString(),
                RoleId = x.RoleId.ToString(),
                RoleName = x.Role.Name,
                ModuleId = x.ModuleId.ToString(),
                CanView = x.CanView,
                CanCreate = x.CanCreate,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanManagePermissions = x.CanManagePermissions
            })
            .ToList();
    }

    public async Task<List<ModulePermissionDto>> GetEffectiveModulePermissionsForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionRepository.ListModulePermissionsForRoleAsync(roleId, cancellationToken);
        return permissions
            .OrderBy(x => x.Role.Name)
            .ThenBy(x => x.ModuleId)
            .Select(x => new ModulePermissionDto
            {
                Id = x.Id.ToString(),
                RoleId = x.RoleId.ToString(),
                RoleName = x.Role.Name,
                ModuleId = x.ModuleId.ToString(),
                CanView = x.CanView,
                CanCreate = x.CanCreate,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanManagePermissions = x.CanManagePermissions
            })
            .ToList();
    }

    public async Task<List<FieldPermissionDto>> GetEffectiveFieldPermissionsAsync(
        Guid moduleId,
        CancellationToken cancellationToken = default)
    {
        var module = await permissionRepository.GetModuleAsync(moduleId, cancellationToken);

        if (module is null)
        {
            return [];
        }

        var fieldNames = ExtractFieldNames(module.Fields);
        var modulePermissions = await permissionRepository.ListModulePermissionsAsync(moduleId, cancellationToken);
        var fieldPermissions = await permissionRepository.ListFieldPermissionsAsync(moduleId, cancellationToken);

        var explicitLookup = fieldPermissions.ToDictionary(
            x => $"{x.RoleId}:{x.FieldName}",
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var result = new List<FieldPermissionDto>();

        foreach (var modulePermission in modulePermissions)
        {
            foreach (var fieldName in fieldNames)
            {
                explicitLookup.TryGetValue($"{modulePermission.RoleId}:{fieldName}", out var explicitPermission);
                result.Add(new FieldPermissionDto
                {
                    Id = explicitPermission?.Id.ToString() ?? string.Empty,
                    RoleId = modulePermission.RoleId.ToString(),
                    RoleName = modulePermission.Role.Name,
                    ModuleId = moduleId.ToString(),
                    FieldName = fieldName,
                    CanView = explicitPermission?.CanView ?? modulePermission.CanView,
                    CanEdit = explicitPermission?.CanEdit ?? modulePermission.CanEdit
                });
            }
        }

        return result;
    }

    public async Task<List<FieldPermissionDto>> GetEffectiveFieldPermissionsForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var modulePermissions = await permissionRepository.ListModulePermissionsForRoleAsync(roleId, cancellationToken);
        var explicitPermissions = await permissionRepository.ListFieldPermissionsForRoleAsync(roleId, cancellationToken);

        var explicitLookup = explicitPermissions.ToDictionary(
            x => $"{x.ModuleId}:{x.FieldName}",
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var result = new List<FieldPermissionDto>();
        foreach (var modulePermission in modulePermissions)
        {
            foreach (var fieldName in ExtractFieldNames(modulePermission.Module.Fields))
            {
                explicitLookup.TryGetValue($"{modulePermission.ModuleId}:{fieldName}", out var explicitPermission);
                result.Add(new FieldPermissionDto
                {
                    Id = explicitPermission?.Id.ToString() ?? string.Empty,
                    RoleId = modulePermission.RoleId.ToString(),
                    RoleName = modulePermission.Role.Name,
                    ModuleId = modulePermission.ModuleId.ToString(),
                    FieldName = fieldName,
                    CanView = explicitPermission?.CanView ?? modulePermission.CanView,
                    CanEdit = explicitPermission?.CanEdit ?? modulePermission.CanEdit
                });
            }
        }

        return result;
    }

    public JsonElement FilterModuleFields(JsonElement fields, ModuleAuthorizationContext authorizationContext)
    {
        var allowedFields = new List<object>();
        if (fields.ValueKind != JsonValueKind.Array)
        {
            return JsonSerializer.SerializeToElement(allowedFields);
        }

        foreach (var field in fields.EnumerateArray())
        {
            if (!field.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var fieldName = nameElement.GetString() ?? string.Empty;
            if (!authorizationContext.CanViewField(fieldName))
            {
                continue;
            }

            allowedFields.Add(JsonSerializer.Deserialize<object>(field.GetRawText())!);
        }

        return JsonSerializer.SerializeToElement(allowedFields);
    }

    public JsonElement FilterEntryData(JsonElement data, ModuleAuthorizationContext authorizationContext)
    {
        var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (data.ValueKind != JsonValueKind.Object)
        {
            return JsonSerializer.SerializeToElement(filtered);
        }

        foreach (var property in data.EnumerateObject())
        {
            if (!authorizationContext.CanViewField(property.Name))
            {
                continue;
            }

            filtered[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
        }

        return JsonSerializer.SerializeToElement(filtered);
    }

    public string? ValidateEditableFields(JsonElement data, ModuleAuthorizationContext authorizationContext)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return "Entry data must be a JSON object.";
        }

        foreach (var property in data.EnumerateObject())
        {
            if (!authorizationContext.CanEditField(property.Name))
            {
                return $"You do not have permission to edit field '{property.Name}'.";
            }
        }

        return null;
    }

    public AuthUserDto BuildAuthUserDto(
        User user,
        string roleName,
        IReadOnlyList<ModulePermissionDto> modulePermissions,
        IReadOnlyList<FieldPermissionDto> fieldPermissions)
    {
        return new AuthUserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FullName = user.FullName,
            RoleId = user.RoleId.ToString(),
            RoleName = roleName,
            IsActive = user.IsActive,
            CanManageRoles = roleName.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase),
            CanManagePermissions = modulePermissions.Any(x => x.CanManagePermissions),
            ModulePermissions = modulePermissions.ToList(),
            FieldPermissions = fieldPermissions.ToList()
        };
    }

    public static Dictionary<string, string> BuildFieldMap(JsonElement fields)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (fields.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var field in fields.EnumerateArray())
        {
            if (!TryGetFieldProperty(field, "name", out var nameElement) ||
                !TryGetFieldProperty(field, "type", out var typeElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            var type = typeElement.GetString();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            map[name] = FieldTypeConstants.NormalizeFieldType(type);
        }

        return map;
    }

    private async Task<bool> CanAccessEntryByVisibilityAsync(
        ModuleAuthorizationContext moduleAuthorization,
        Entry entry,
        bool allowEdit,
        CancellationToken cancellationToken)
    {
        if (moduleAuthorization.UserContext.IsOwnerOrAdmin ||
            entry.CreatedByUserId == moduleAuthorization.UserContext.User.Id)
        {
            return true;
        }

        var permission = await permissionRepository.GetEntryVisibilityPermissionAsync(
            entry.Id,
            moduleAuthorization.UserContext.Role.Id,
            cancellationToken);

        if (permission is null)
        {
            return false;
        }

        return allowEdit ? permission.CanEdit : permission.CanView;
    }

    private static bool IsAllowed(ModulePermission permission, ModuleAction action)
    {
        return action switch
        {
            ModuleAction.View => permission.CanView,
            ModuleAction.Create => permission.CanCreate,
            ModuleAction.Edit => permission.CanEdit,
            ModuleAction.Delete => permission.CanDelete,
            ModuleAction.ManagePermissions => permission.CanManagePermissions,
            _ => false
        };
    }

    private static List<string> ExtractFieldNames(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return fields.EnumerateArray()
            .Select(x => TryGetFieldProperty(x, "name", out var property) ? property.GetString() : null)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
    }

    private static bool TryGetFieldProperty(JsonElement field, string propertyName, out JsonElement propertyValue)
    {
        foreach (var property in field.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = default;
        return false;
    }
}
