namespace BusinessOps.Backend.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool CanManageRoles { get; set; }

    public bool CanManagePermissions { get; set; }

    public List<ModulePermissionDto> ModulePermissions { get; set; } = new();

    public List<FieldPermissionDto> FieldPermissions { get; set; } = new();
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public AuthUserDto User { get; set; } = new();
}
