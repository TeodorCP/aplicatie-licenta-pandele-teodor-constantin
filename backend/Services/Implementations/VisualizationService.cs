using System.Security.Claims;
using System.Text.Json;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class VisualizationService(
    IVisualizationRepository visualizationRepository,
    IModuleRepository moduleRepository,
    PermissionService permissionService) : IVisualizationService
{
    public async Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, CreateVisualizationRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, request.ModuleId, ModuleAction.Create, cancellationToken);
        if (authorizationContext is null)
        {
            return await moduleRepository.ExistsAsync(request.ModuleId, cancellationToken)
                ? ServiceResult<object>.Forbidden()
                : ServiceResult<object>.NotFound("Module not found.");
        }

        var xField = ResolveXField(request.XField, request.FieldName);
        var yField = ResolveYField(request.YField, request.SecondaryFieldName);
        if (!HasFieldAccess(authorizationContext, xField, yField))
        {
            return ServiceResult<object>.Forbidden();
        }

        var now = DateTime.UtcNow;
        var visualization = new Visualization
        {
            Id = Guid.NewGuid(),
            ModuleId = request.ModuleId,
            Title = request.Title.Trim(),
            XField = xField,
            XAggregation = NormalizeOptional(request.XAggregation ?? request.AggregationType),
            YField = yField,
            FieldName = xField,
            SecondaryFieldName = yField,
            ChartType = NormalizeChartType(request.ChartType),
            WidgetSize = NormalizeWidgetSize(request.WidgetSize),
            AggregationType = NormalizeOptional(request.AggregationType ?? request.XAggregation),
            DateRange = NormalizeDateRangeType(request.DateRange ?? request.DateRangeType),
            DateRangeType = NormalizeDateRangeType(request.DateRangeType ?? request.DateRange),
            CustomStartTimestamp = NormalizeCustomTimestamp(request.CustomStartTimestamp),
            CustomEndTimestamp = NormalizeCustomTimestamp(request.CustomEndTimestamp),
            SummaryMetric = NormalizeSummaryMetric(request.SummaryMetric),
            Description = NormalizeDescription(request.Description),
            GeneralOptions = SerializeJson(request.GeneralOptions),
            ChartSpecificOptions = SerializeJson(request.ChartSpecificOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        await visualizationRepository.AddAsync(visualization, cancellationToken);
        await visualizationRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<object>.Created(ToResponse(visualization, authorizationContext.Module.Name));
    }

    public async Task<ServiceResult<List<object>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var visualizations = await visualizationRepository.ListAsync(cancellationToken);
        var moduleNames = await visualizationRepository.GetModuleNamesAsync(cancellationToken);

        var response = new List<object>();
        foreach (var visualization in visualizations)
        {
            var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.View, cancellationToken);
            if (authorizationContext is null || !HasFieldAccess(authorizationContext, visualization.XField ?? visualization.FieldName, visualization.YField ?? visualization.SecondaryFieldName))
            {
                continue;
            }

            response.Add(ToResponse(visualization, moduleNames.TryGetValue(visualization.ModuleId, out var name) ? name : null));
        }

        return ServiceResult<List<object>>.Ok(response);
    }

    public async Task<ServiceResult<object>> ListByModuleAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken)
    {
        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, moduleId, ModuleAction.View, cancellationToken);
        if (authorizationContext is null)
        {
            return await moduleRepository.ExistsAsync(moduleId, cancellationToken)
                ? ServiceResult<object>.Forbidden()
                : ServiceResult<object>.NotFound("Module not found.");
        }

        var visualizations = await visualizationRepository.ListByModuleAsync(moduleId, cancellationToken);
        return ServiceResult<object>.Ok(visualizations
            .Where(x => HasFieldAccess(authorizationContext, x.XField ?? x.FieldName, x.YField ?? x.SecondaryFieldName))
            .Select(x => ToResponse(x, null))
            .ToList());
    }

    public async Task<ServiceResult<object>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var visualization = await visualizationRepository.GetByIdAsync(id, cancellationToken);
        if (visualization is null)
        {
            return ServiceResult<object>.NotFound("Visualization not found.");
        }

        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.View, cancellationToken);
        if (authorizationContext is null || !HasFieldAccess(authorizationContext, visualization.XField ?? visualization.FieldName, visualization.YField ?? visualization.SecondaryFieldName))
        {
            return ServiceResult<object>.Forbidden();
        }

        return ServiceResult<object>.Ok(ToResponse(visualization));
    }

    public async Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateVisualizationRequestDto request, CancellationToken cancellationToken)
    {
        var visualization = await visualizationRepository.GetByIdAsync(id, cancellationToken);
        if (visualization is null)
        {
            return ServiceResult<object>.NotFound("Visualization not found.");
        }

        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.Edit, cancellationToken);
        if (authorizationContext is null)
        {
            return ServiceResult<object>.Forbidden();
        }

        var targetModuleId = request.ModuleId is Guid moduleId && moduleId != Guid.Empty ? moduleId : visualization.ModuleId;
        ModuleAuthorizationContext targetAuthorizationContext;
        if (targetModuleId == visualization.ModuleId)
        {
            targetAuthorizationContext = authorizationContext;
        }
        else
        {
            var targetAuthorization = await permissionService.AuthorizeModuleAsync(principal, targetModuleId, ModuleAction.Create, cancellationToken);
            if (targetAuthorization is null)
            {
                return await moduleRepository.ExistsAsync(targetModuleId, cancellationToken)
                    ? ServiceResult<object>.Forbidden()
                    : ServiceResult<object>.NotFound("Module not found.");
            }

            targetAuthorizationContext = targetAuthorization;
        }

        var validationError = ValidateCommonFields(request.Title, request.XField ?? request.FieldName, request.ChartType, request.DateRangeType ?? request.DateRange, request.CustomStartTimestamp, request.CustomEndTimestamp);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var xField = ResolveXField(request.XField, request.FieldName);
        var yField = ResolveYField(request.YField, request.SecondaryFieldName);
        if (!HasFieldAccess(targetAuthorizationContext, xField, yField))
        {
            return ServiceResult<object>.Forbidden();
        }

        visualization.ModuleId = targetModuleId;
        visualization.Title = request.Title.Trim();
        visualization.XField = xField;
        visualization.XAggregation = NormalizeOptional(request.XAggregation ?? request.AggregationType);
        visualization.YField = yField;
        visualization.FieldName = xField;
        visualization.SecondaryFieldName = yField;
        visualization.ChartType = NormalizeChartType(request.ChartType);
        visualization.WidgetSize = NormalizeWidgetSize(request.WidgetSize);
        visualization.AggregationType = NormalizeOptional(request.AggregationType ?? request.XAggregation);
        visualization.DateRange = NormalizeDateRangeType(request.DateRange ?? request.DateRangeType);
        visualization.DateRangeType = NormalizeDateRangeType(request.DateRangeType ?? request.DateRange);
        visualization.CustomStartTimestamp = NormalizeCustomTimestamp(request.CustomStartTimestamp);
        visualization.CustomEndTimestamp = NormalizeCustomTimestamp(request.CustomEndTimestamp);
        visualization.SummaryMetric = NormalizeSummaryMetric(request.SummaryMetric);
        visualization.Description = NormalizeDescription(request.Description);
        visualization.GeneralOptions = SerializeJson(request.GeneralOptions);
        visualization.ChartSpecificOptions = SerializeJson(request.ChartSpecificOptions);
        visualization.UpdatedAt = DateTime.UtcNow;

        await visualizationRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Ok(ToResponse(visualization, targetAuthorizationContext.Module.Name));
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var visualization = await visualizationRepository.GetByIdAsync(id, cancellationToken);
        if (visualization is null)
        {
            return ServiceResult<object>.NotFound("Visualization not found.");
        }

        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.Delete, cancellationToken);
        if (authorizationContext is null)
        {
            return ServiceResult<object>.Forbidden();
        }

        visualizationRepository.Remove(visualization);
        await visualizationRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    private static string? ValidateCreateRequest(CreateVisualizationRequestDto request)
    {
        if (request.ModuleId == Guid.Empty)
        {
            return "ModuleId is required.";
        }

        return ValidateCommonFields(request.Title, request.XField ?? request.FieldName, request.ChartType, request.DateRangeType ?? request.DateRange, request.CustomStartTimestamp, request.CustomEndTimestamp);
    }

    private static string? ValidateCommonFields(string? title, string? xField, string? chartType, string? dateRangeType, DateTime? customStartTimestamp, DateTime? customEndTimestamp)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
        if (string.IsNullOrWhiteSpace(xField)) return "XField is required.";
        if (string.IsNullOrWhiteSpace(chartType)) return "ChartType is required.";
        var normalizedDateRange = NormalizeDateRangeType(dateRangeType);
        if (normalizedDateRange == "custom")
        {
            if (customStartTimestamp is null || customEndTimestamp is null) return "Custom start and end timestamps are required.";
            if (customStartTimestamp > customEndTimestamp) return "Custom start timestamp must be before the end timestamp.";
        }
        return null;
    }

    private static string NormalizeChartType(string? value)
    {
        var normalized = (value ?? "bar").Trim().ToLowerInvariant();
        return normalized == "summary_card" ? "summary" : normalized;
    }

    private static string NormalizeWidgetSize(string? value)
    {
        var normalized = (value ?? "medium").Trim().ToLowerInvariant();
        return normalized is "small" or "medium" or "large" ? normalized : "medium";
    }

    private static string NormalizeDateRangeType(string? value)
    {
        var normalized = NormalizeOptional(value)?.ToLowerInvariant();
        return normalized switch
        {
            null => "all",
            "7d" or "30d" or "90d" or "year" or "all" or "custom" => normalized,
            _ => "all"
        };
    }

    private static string? NormalizeSummaryMetric(string? value)
    {
        var normalized = NormalizeOptional(value)?.ToLowerInvariant();
        return normalized is null ? null : normalized switch
        {
            "average" => "avg",
            "sum" or "avg" or "min" or "max" or "count" => normalized,
            _ => "sum"
        };
    }

    private static DateTime? NormalizeCustomTimestamp(DateTime? value)
    {
        return value?.ToUniversalTime();
    }

    private static string? NormalizeDescription(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null) return null;
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveXField(string? xField, string? fieldName)
    {
        return (NormalizeOptional(xField) ?? NormalizeOptional(fieldName) ?? string.Empty).Trim();
    }

    private static string? ResolveYField(string? yField, string? secondaryFieldName)
    {
        return NormalizeOptional(yField) ?? NormalizeOptional(secondaryFieldName);
    }

    private static string? SerializeJson(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return value.Value.GetRawText();
    }

    private static JsonElement? DeserializeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(value);
        }
        catch
        {
            return null;
        }
    }

    private static object ToResponse(Visualization visualization, string? moduleName = null)
    {
        var effectiveXField = visualization.XField ?? visualization.FieldName;
        var effectiveYField = visualization.YField ?? visualization.SecondaryFieldName;
        var effectiveAggregation = visualization.XAggregation ?? visualization.AggregationType;
        var effectiveDateRange = visualization.DateRangeType ?? visualization.DateRange ?? "all";
        return new
        {
            visualization.Id,
            visualization.ModuleId,
            Module = moduleName is null ? null : new { Name = moduleName },
            visualization.Title,
            XField = effectiveXField,
            XAggregation = effectiveAggregation,
            YField = effectiveYField,
            visualization.FieldName,
            visualization.SecondaryFieldName,
            visualization.ChartType,
            visualization.WidgetSize,
            AggregationType = effectiveAggregation,
            DateRangeType = effectiveDateRange,
            DateRange = effectiveDateRange,
            visualization.CustomStartTimestamp,
            visualization.CustomEndTimestamp,
            visualization.SummaryMetric,
            visualization.Description,
            GeneralOptions = DeserializeJson(visualization.GeneralOptions),
            ChartSpecificOptions = DeserializeJson(visualization.ChartSpecificOptions),
            visualization.CreatedAt,
            visualization.UpdatedAt
        };
    }

    private static bool HasFieldAccess(ModuleAuthorizationContext authorizationContext, string fieldName, string? secondaryFieldName)
    {
        return authorizationContext.CanViewField(fieldName) &&
            (string.IsNullOrWhiteSpace(secondaryFieldName) || authorizationContext.CanViewField(secondaryFieldName));
    }
}
