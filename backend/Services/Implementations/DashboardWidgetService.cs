using System.Security.Claims;
using System.Text.Json;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class DashboardWidgetService(
    IDashboardWidgetRepository dashboardWidgetRepository,
    IVisualizationRepository visualizationRepository,
    PermissionService permissionService) : IDashboardWidgetService
{
    public async Task<ServiceResult<List<object>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var widgets = await dashboardWidgetRepository.ListAsync(cancellationToken);
        var response = new List<object>();
        foreach (var widget in widgets)
        {
            if (await CanAccessWidgetAsync(principal, widget, cancellationToken))
            {
                response.Add(ToResponse(widget));
            }
        }

        return ServiceResult<List<object>>.Ok(response);
    }

    public async Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, CreateDashboardWidgetRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequestAsync(principal, request.Type, request.Title, request.Size, request.Width, request.Height, request.X, request.Y, request.ModuleId, request.VisualizationId, cancellationToken);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var (width, height) = ResolveDimensions(request.Size, request.Width, request.Height);
        var now = DateTime.UtcNow;
        var widget = new DashboardWidget
        {
            Id = Guid.NewGuid(),
            Type = NormalizeType(request.Type),
            Title = request.Title.Trim(),
            ModuleId = request.ModuleId,
            VisualizationId = request.VisualizationId,
            Size = NormalizeSize(request.Size),
            Width = width,
            Height = height,
            X = NormalizeAxis(request.X, width),
            Y = NormalizeVerticalAxis(request.Y),
            Position = request.Position,
            VisualSettings = SerializeJson(request.VisualSettings),
            CreatedAt = now,
            UpdatedAt = now
        };

        await dashboardWidgetRepository.AddAsync(widget, cancellationToken);
        await dashboardWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Created(ToResponse(widget));
    }

    public async Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateDashboardWidgetRequestDto request, CancellationToken cancellationToken)
    {
        var widget = await dashboardWidgetRepository.GetByIdAsync(id, cancellationToken);
        if (widget is null)
        {
            return ServiceResult<object>.NotFound("Dashboard widget not found.");
        }

        var validationError = await ValidateRequestAsync(principal, request.Type, request.Title, request.Size, request.Width, request.Height, request.X, request.Y, request.ModuleId, request.VisualizationId, cancellationToken);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var (width, height) = ResolveDimensions(request.Size, request.Width, request.Height);
        widget.Type = NormalizeType(request.Type);
        widget.Title = request.Title.Trim();
        widget.ModuleId = request.ModuleId;
        widget.VisualizationId = request.VisualizationId;
        widget.Size = NormalizeSize(request.Size);
        widget.Width = width;
        widget.Height = height;
        widget.X = NormalizeAxis(request.X, width);
        widget.Y = NormalizeVerticalAxis(request.Y);
        widget.Position = request.Position;
        widget.VisualSettings = SerializeJson(request.VisualSettings);
        widget.UpdatedAt = DateTime.UtcNow;

        await dashboardWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Ok(ToResponse(widget));
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var widget = await dashboardWidgetRepository.GetByIdAsync(id, cancellationToken);
        if (widget is null)
        {
            return ServiceResult<object>.NotFound("Dashboard widget not found.");
        }

        dashboardWidgetRepository.Remove(widget);
        await dashboardWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    private async Task<string?> ValidateRequestAsync(ClaimsPrincipal principal, string? type, string? title, string? size, int? width, int? height, int? x, int? y, Guid? moduleId, Guid? visualizationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Type is required.";
        if (string.IsNullOrWhiteSpace(title)) return "Title is required.";

        var normalizedType = NormalizeType(type);
        if (normalizedType is not ("visualization" or "calendar_summary" or "tracker_summary" or "recent_activity" or "analytics"))
        {
            return "Type must be one of: visualization, calendar_summary, tracker_summary, recent_activity, analytics.";
        }

        if (NormalizeSize(size) is not ("small" or "medium" or "large" or "full-width")) return "Size must be small, medium, large, or full-width.";
        if (width is not null && (width < 1 || width > 12)) return "Width must be between 1 and 12.";
        if (height is not null && (height < 1 || height > 12)) return "Height must be between 1 and 12.";
        if (x is not null && (x < 1 || x > 12)) return "X must be between 1 and 12.";
        if (y is not null && y < 1) return "Y must be greater than 0.";

        if (normalizedType == "visualization")
        {
            if (visualizationId is null || visualizationId == Guid.Empty) return "VisualizationId is required for visualization widget.";
            var visualization = await visualizationRepository.GetByIdAsync(visualizationId.Value, cancellationToken);
            if (visualization is null) return "Visualization not found.";
            if (await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.View, cancellationToken) is null)
            {
                return "You do not have access to this visualization.";
            }
        }

        if (normalizedType == "tracker_summary")
        {
            if (moduleId is null || moduleId == Guid.Empty) return "ModuleId is required for tracker_summary widget.";
            if (await permissionService.AuthorizeModuleAsync(principal, moduleId.Value, ModuleAction.View, cancellationToken) is null)
            {
                return "You do not have access to this module.";
            }
        }

        return null;
    }

    private async Task<bool> CanAccessWidgetAsync(ClaimsPrincipal principal, DashboardWidget widget, CancellationToken cancellationToken)
    {
        if (widget.VisualizationId is Guid visualizationId)
        {
            var visualization = await visualizationRepository.GetByIdAsync(visualizationId, cancellationToken);
            return visualization is not null &&
                await permissionService.AuthorizeModuleAsync(principal, visualization.ModuleId, ModuleAction.View, cancellationToken) is not null;
        }

        if (widget.ModuleId is Guid moduleId)
        {
            return await permissionService.AuthorizeModuleAsync(principal, moduleId, ModuleAction.View, cancellationToken) is not null;
        }

        return true;
    }

    private static string NormalizeType(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeSize(string? value)
    {
        var normalized = (value ?? "medium").Trim().ToLowerInvariant();
        return normalized is "small" or "medium" or "large" or "full-width" ? normalized : "medium";
    }

    private static (int width, int height) ResolveDimensions(string? size, int? width, int? height)
    {
        if (width is not null && height is not null) return (width.Value, height.Value);

        return NormalizeSize(size) switch
        {
            "small" => (3, 2),
            "large" => (9, 4),
            "full-width" => (12, 4),
            _ => (6, 3)
        };
    }

    private static int? NormalizeAxis(int? x, int width)
    {
        if (x is null) return null;
        var maxStart = Math.Max(1, 13 - width);
        return Math.Max(1, Math.Min(maxStart, x.Value));
    }

    private static int? NormalizeVerticalAxis(int? y)
    {
        if (y is null) return null;
        return Math.Max(1, y.Value);
    }

    private static string? SerializeJson(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
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

    private static object ToResponse(DashboardWidget widget)
    {
        return new
        {
            widget.Id,
            widget.Type,
            widget.Title,
            widget.ModuleId,
            widget.VisualizationId,
            widget.Size,
            widget.Width,
            widget.Height,
            widget.X,
            widget.Y,
            widget.Position,
            VisualSettings = DeserializeJson(widget.VisualSettings),
            widget.CreatedAt,
            widget.UpdatedAt
        };
    }
}
