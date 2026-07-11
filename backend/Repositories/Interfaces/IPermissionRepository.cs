using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IPermissionRepository
{
    Task<User?> GetActiveUserWithRoleAsync(Guid userId, CancellationToken cancellationToken);

    Task<Module?> GetModuleAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<ModulePermission?> GetModulePermissionAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken);

    Task<List<FieldPermission>> GetFieldPermissionsAsync(Guid roleId, Guid moduleId, CancellationToken cancellationToken);

    Task<List<EntryVisibilityPermission>> GetEntryVisibilityPermissionsForEntriesAsync(List<Guid> entryIds, Guid roleId, CancellationToken cancellationToken);

    Task<List<EntryVisibilityPermission>> GetEntryVisibilityPermissionsAsync(Guid entryId, CancellationToken cancellationToken);

    Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken);

    Task<List<Guid>> GetViewableModuleIdsAsync(Guid roleId, CancellationToken cancellationToken);

    Task<List<ModulePermission>> ListModulePermissionsAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<List<ModulePermission>> ListModulePermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task<List<FieldPermission>> ListFieldPermissionsAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<List<FieldPermission>> ListFieldPermissionsForRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task<Dictionary<Guid, Role>> GetRolesByIdAsync(CancellationToken cancellationToken);

    Task<List<ModulePermission>> GetMutableModulePermissionsAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<List<FieldPermission>> GetMutableFieldPermissionsAsync(Guid moduleId, CancellationToken cancellationToken);

    Task<EntryVisibilityPermission?> GetEntryVisibilityPermissionAsync(Guid entryId, Guid roleId, CancellationToken cancellationToken);

    Task AddModulePermissionAsync(ModulePermission permission, CancellationToken cancellationToken);

    Task AddFieldPermissionAsync(FieldPermission permission, CancellationToken cancellationToken);

    Task AddEntryVisibilityPermissionAsync(EntryVisibilityPermission permission, CancellationToken cancellationToken);

    void RemoveEntryVisibilityPermissions(IEnumerable<EntryVisibilityPermission> permissions);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
