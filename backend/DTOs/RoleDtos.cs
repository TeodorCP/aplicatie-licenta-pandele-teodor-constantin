namespace BusinessOps.Backend.DTOs;

public class RoleDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsSystemRole { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateRoleRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateRoleRequestDto
{
    public string Name { get; set; } = string.Empty;
}
