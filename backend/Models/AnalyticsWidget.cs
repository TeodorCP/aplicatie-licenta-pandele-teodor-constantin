namespace BusinessOps.Backend.Models;

public class AnalyticsWidget
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ModuleAId { get; set; }
    public string FieldAName { get; set; } = string.Empty;
    public Guid ModuleBId { get; set; }
    public string FieldBName { get; set; } = string.Empty;
    public string ChartType { get; set; } = "scatter";
    public string? DateRange { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

