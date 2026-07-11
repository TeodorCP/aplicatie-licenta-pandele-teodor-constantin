using System.Text.Json;

namespace BusinessOps.Backend.Models;

public class Module
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public JsonElement Fields { get; set; } = JsonSerializer.Deserialize<JsonElement>("[]");

    public DateTime CreatedAt { get; set; }

    public List<Entry> Entries { get; set; } = new();

    public List<Visualization> Visualizations { get; set; } = new();

    public List<DashboardWidget> DashboardWidgets { get; set; } = new();

    public List<ModulePermission> ModulePermissions { get; set; } = new();

    public List<FieldPermission> FieldPermissions { get; set; } = new();
}
