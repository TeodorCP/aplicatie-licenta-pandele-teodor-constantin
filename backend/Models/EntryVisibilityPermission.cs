namespace BusinessOps.Backend.Models;

public class EntryVisibilityPermission
{
    public Guid Id { get; set; }

    public Guid EntryId { get; set; }

    public Entry Entry { get; set; } = default!;

    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = default!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = default!;

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }
}
