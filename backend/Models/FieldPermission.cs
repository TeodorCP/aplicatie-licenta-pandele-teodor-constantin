namespace BusinessOps.Backend.Models;

public class FieldPermission
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = default!;

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = default!;

    public string FieldName { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }
}
