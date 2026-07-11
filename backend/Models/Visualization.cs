namespace BusinessOps.Backend.Models;

public class Visualization
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = default!;

    public string Title { get; set; } = string.Empty;

    public string? XField { get; set; }

    public string? XAggregation { get; set; }

    public string? YField { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? SecondaryFieldName { get; set; }

    public string ChartType { get; set; } = string.Empty;

    public string WidgetSize { get; set; } = "medium";

    public string? AggregationType { get; set; }

    public string? DateRange { get; set; }

    public string? DateRangeType { get; set; }

    public DateTime? CustomStartTimestamp { get; set; }

    public DateTime? CustomEndTimestamp { get; set; }

    public string? SummaryMetric { get; set; }

    public string? Description { get; set; }

    public string? GeneralOptions { get; set; }

    public string? ChartSpecificOptions { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<DashboardWidget> DashboardWidgets { get; set; } = new();
}
