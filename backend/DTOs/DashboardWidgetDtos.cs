using System.Text.Json;

namespace BusinessOps.Backend.DTOs;

public class CreateDashboardWidgetRequestDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public Guid? ModuleId { get; set; }

    public Guid? VisualizationId { get; set; }

    public string Size { get; set; } = "medium";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }

    public int Position { get; set; }

    public JsonElement? VisualSettings { get; set; }
}

public class UpdateDashboardWidgetRequestDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public Guid? ModuleId { get; set; }

    public Guid? VisualizationId { get; set; }

    public string Size { get; set; } = "medium";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }

    public int Position { get; set; }

    public JsonElement? VisualSettings { get; set; }
}
