using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class PermissionRepository(AppDbContext dbContext) : IPermissionRepository
{
    public Task<User?> GetActiveUserWithRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public Task<Module?> GetModuleAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.Modules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);
    }

    public Task<ModulePermission?> GetModulePermissionAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.ModuleId == moduleId, cancellationToken);
    }

    public Task<List<FieldPermission>> GetFieldPermissionsAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.FieldPermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId && x.ModuleId == moduleId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<EntryVisibilityPermission>> GetEntryVisibilityPermissionsForEntriesAsync(List<Guid> entryIds, Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.EntryVisibilityPermissions
            .AsNoTracking()
            .Where(x => entryIds.Contains(x.EntryId) && x.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<EntryVisibilityPermission>> GetEntryVisibilityPermissionsAsync(Guid entryId, CancellationToken cancellationToken)
    {
        return dbContext.EntryVisibilityPermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.EntryId == entryId)
            .OrderBy(x => x.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken)
    {
        return dbContext.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Guid>> GetViewableModuleIdsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId && x.CanView)
            .Select(x => x.ModuleId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ModulePermission>> ListModulePermissionsAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.ModuleId == moduleId)
            .OrderBy(x => x.Role.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ModulePermission>> ListModulePermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Module)
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<FieldPermission>> ListFieldPermissionsAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.FieldPermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.ModuleId == moduleId)
            .OrderBy(x => x.Role.Name)
            .ThenBy(x => x.FieldName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<FieldPermission>> ListFieldPermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.FieldPermissions
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Module)
            .Where(x => x.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public Task<Dictionary<Guid, Role>> GetRolesByIdAsync(CancellationToken cancellationToken)
    {
        return dbContext.Roles.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public Task<List<ModulePermission>> GetMutableModulePermissionsAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions.Where(x => x.ModuleId == moduleId).ToListAsync(cancellationToken);
    }

    public Task<List<FieldPermission>> GetMutableFieldPermissionsAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        return dbContext.FieldPermissions.Where(x => x.ModuleId == moduleId).ToListAsync(cancellationToken);
    }

    public Task<EntryVisibilityPermission?> GetEntryVisibilityPermissionAsync(Guid entryId, Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.EntryVisibilityPermissions
            .FirstOrDefaultAsync(x => x.EntryId == entryId && x.RoleId == roleId, cancellationToken);
    }

    public Task AddModulePermissionAsync(ModulePermission permission, CancellationToken cancellationToken)
    {
        return dbContext.ModulePermissions.AddAsync(permission, cancellationToken).AsTask();
    }

    public Task AddFieldPermissionAsync(FieldPermission permission, CancellationToken cancellationToken)
    {
        return dbContext.FieldPermissions.AddAsync(permission, cancellationToken).AsTask();
    }

    public Task AddEntryVisibilityPermissionAsync(EntryVisibilityPermission permission, CancellationToken cancellationToken)
    {
        return dbContext.EntryVisibilityPermissions.AddAsync(permission, cancellationToken).AsTask();
    }

    public void RemoveEntryVisibilityPermissions(IEnumerable<EntryVisibilityPermission> permissions)
    {
        dbContext.EntryVisibilityPermissions.RemoveRange(permissions);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
