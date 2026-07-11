using System.Text.Json;

namespace BusinessOps.Backend.Models;

public class Entry
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = default!;

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = default!;

    public DateTime Timestamp { get; set; }

    public JsonElement Data { get; set; } = JsonSerializer.Deserialize<JsonElement>("{}");

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<EntryVisibilityPermission> VisibilityPermissions { get; set; } = new();
}
