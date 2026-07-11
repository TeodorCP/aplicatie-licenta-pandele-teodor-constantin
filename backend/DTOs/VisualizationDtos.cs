using System.Text.Json;

namespace BusinessOps.Backend.DTOs;

public class CreateVisualizationRequestDto
{
    public Guid ModuleId { get; set; }

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

    public JsonElement? GeneralOptions { get; set; }

    public JsonElement? ChartSpecificOptions { get; set; }
}

public class UpdateVisualizationRequestDto
{
    public Guid? ModuleId { get; set; }

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

    public JsonElement? GeneralOptions { get; set; }

    public JsonElement? ChartSpecificOptions { get; set; }
}
