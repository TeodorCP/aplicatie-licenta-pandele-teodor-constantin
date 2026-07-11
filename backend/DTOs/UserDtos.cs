namespace BusinessOps.Backend.DTOs;

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequestDto
{
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class ResetPasswordRequestDto
{
    public string? TemporaryPassword { get; set; }
}
