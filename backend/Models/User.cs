namespace BusinessOps.Backend.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<Entry> CreatedEntries { get; set; } = new();

    public List<EntryVisibilityPermission> EntryVisibilityPermissions { get; set; } = new();
}
