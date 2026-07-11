namespace BusinessOps.Backend.Models;

public class DashboardWidget
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public Guid? ModuleId { get; set; }

    public Module? Module { get; set; }

    public Guid? VisualizationId { get; set; }

    public Visualization? Visualization { get; set; }

    public string Size { get; set; } = "medium";
    public int Width { get; set; } = 6;
    public int Height { get; set; } = 3;
    public int? X { get; set; }
    public int? Y { get; set; }

    public int Position { get; set; }

    public string? VisualSettings { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
