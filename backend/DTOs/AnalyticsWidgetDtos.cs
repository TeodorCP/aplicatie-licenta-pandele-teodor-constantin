namespace BusinessOps.Backend.DTOs;

public class CreateAnalyticsWidgetRequestDto
{
    public string Title { get; set; } = string.Empty;
    public Guid ModuleAId { get; set; }
    public string FieldAName { get; set; } = string.Empty;
    public Guid ModuleBId { get; set; }
    public string FieldBName { get; set; } = string.Empty;
    public string ChartType { get; set; } = "scatter";
    public string? DateRange { get; set; }
}

public class UpdateAnalyticsWidgetRequestDto
{
    public string Title { get; set; } = string.Empty;
    public Guid ModuleAId { get; set; }
    public string FieldAName { get; set; } = string.Empty;
    public Guid ModuleBId { get; set; }
    public string FieldBName { get; set; } = string.Empty;
    public string ChartType { get; set; } = "scatter";
    public string? DateRange { get; set; }
}

