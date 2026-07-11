namespace BusinessOps.Backend.DTOs;

public class ModulePermissionDto
{
    public string Id { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string ModuleId { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public bool CanManagePermissions { get; set; }
}

public class UpdateModulePermissionsRequestDto
{
    public List<ModulePermissionDto> Permissions { get; set; } = new();
}

public class FieldPermissionDto
{
    public string Id { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string ModuleId { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }
}

public class UpdateFieldPermissionsRequestDto
{
    public List<FieldPermissionDto> Permissions { get; set; } = new();
}
