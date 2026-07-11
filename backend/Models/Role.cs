namespace BusinessOps.Backend.Models;

public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<User> Users { get; set; } = new();

    public List<ModulePermission> ModulePermissions { get; set; } = new();

    public List<FieldPermission> FieldPermissions { get; set; } = new();

    public List<EntryVisibilityPermission> EntryVisibilityPermissions { get; set; } = new();
}
