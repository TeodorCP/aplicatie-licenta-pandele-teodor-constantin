namespace BusinessOps.Backend.Models;

public class ModulePermission
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = default!;

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = default!;

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public bool CanManagePermissions { get; set; }
}
