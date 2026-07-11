using System.Text.Json;

namespace BusinessOps.Backend.DTOs;

public class CreateEntryRequestDto
{
    public DateTime? Timestamp { get; set; }

    public JsonElement Data { get; set; }

    public List<EntryVisibilityPermissionUpdateDto> VisibilityPermissions { get; set; } = new();
}

public class UpdateEntryRequestDto
{
    public DateTime? Timestamp { get; set; }

    public JsonElement Data { get; set; }
}

public class EntryVisibilityPermissionDto
{
    public string Id { get; set; } = string.Empty;

    public string EntryId { get; set; } = string.Empty;

    public string OwnerUserId { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }
}

public class EntryVisibilityPermissionUpdateDto
{
    public string RoleId { get; set; } = string.Empty;

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }
}

public class UpdateEntryVisibilityRequestDto
{
    public List<EntryVisibilityPermissionUpdateDto> Permissions { get; set; } = new();
}
