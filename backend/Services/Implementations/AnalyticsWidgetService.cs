using System.Security.Claims;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class AnalyticsWidgetService(
    IAnalyticsWidgetRepository analyticsWidgetRepository,
    PermissionService permissionService) : IAnalyticsWidgetService
{
    public async Task<ServiceResult<List<AnalyticsWidget>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var items = await analyticsWidgetRepository.ListAsync(cancellationToken);
        var response = new List<AnalyticsWidget>();
        foreach (var item in items)
        {
            if (await CanAccessAnalyticsAsync(principal, item, cancellationToken))
            {
                response.Add(item);
            }
        }

        return ServiceResult<List<AnalyticsWidget>>.Ok(response);
    }

    public async Task<ServiceResult<AnalyticsWidget>> CreateAsync(ClaimsPrincipal principal, CreateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request.ModuleAId, request.ModuleBId, request.FieldAName, request.FieldBName);
        if (validationError is not null)
        {
            return ServiceResult<AnalyticsWidget>.BadRequest(validationError);
        }

        if (!await CanManageAnalyticsAsync(principal, request.ModuleAId, request.ModuleBId, request.FieldAName, request.FieldBName, ModuleAction.Create, cancellationToken))
        {
            return ServiceResult<AnalyticsWidget>.Forbidden();
        }

        var now = DateTime.UtcNow;
        var item = new AnalyticsWidget
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Cross-Module Analytics" : request.Title.Trim(),
            ModuleAId = request.ModuleAId,
            FieldAName = request.FieldAName.Trim(),
            ModuleBId = request.ModuleBId,
            FieldBName = request.FieldBName.Trim(),
            ChartType = string.IsNullOrWhiteSpace(request.ChartType) ? "scatter" : request.ChartType.Trim().ToLowerInvariant(),
            DateRange = string.IsNullOrWhiteSpace(request.DateRange) ? null : request.DateRange.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await analyticsWidgetRepository.AddAsync(item, cancellationToken);
        await analyticsWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<AnalyticsWidget>.Created(item);
    }

    public async Task<ServiceResult<AnalyticsWidget>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken)
    {
        var item = await analyticsWidgetRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<AnalyticsWidget>.NotFound("Analytics widget not found.");
        }

        var validationError = ValidateRequest(request.ModuleAId, request.ModuleBId, request.FieldAName, request.FieldBName);
        if (validationError is not null)
        {
            return ServiceResult<AnalyticsWidget>.BadRequest(validationError);
        }

        if (!await CanManageAnalyticsAsync(principal, request.ModuleAId, request.ModuleBId, request.FieldAName, request.FieldBName, ModuleAction.Edit, cancellationToken))
        {
            return ServiceResult<AnalyticsWidget>.Forbidden();
        }

        item.Title = string.IsNullOrWhiteSpace(request.Title) ? item.Title : request.Title.Trim();
        item.ModuleAId = request.ModuleAId;
        item.FieldAName = request.FieldAName.Trim();
        item.ModuleBId = request.ModuleBId;
        item.FieldBName = request.FieldBName.Trim();
        item.ChartType = string.IsNullOrWhiteSpace(request.ChartType) ? item.ChartType : request.ChartType.Trim().ToLowerInvariant();
        item.DateRange = string.IsNullOrWhiteSpace(request.DateRange) ? null : request.DateRange.Trim();
        item.UpdatedAt = DateTime.UtcNow;

        await analyticsWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<AnalyticsWidget>.Ok(item);
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var item = await analyticsWidgetRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return ServiceResult<object>.NotFound("Analytics widget not found.");
        }

        if (!await CanManageAnalyticsAsync(principal, item.ModuleAId, item.ModuleBId, item.FieldAName, item.FieldBName, ModuleAction.Delete, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        analyticsWidgetRepository.Remove(item);
        await analyticsWidgetRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    private static string? ValidateRequest(Guid moduleAId, Guid moduleBId, string fieldAName, string fieldBName)
    {
        if (moduleAId == Guid.Empty || moduleBId == Guid.Empty) return "ModuleAId and ModuleBId are required.";
        if (string.IsNullOrWhiteSpace(fieldAName) || string.IsNullOrWhiteSpace(fieldBName)) return "FieldAName and FieldBName are required.";
        return null;
    }

    private async Task<bool> CanAccessAnalyticsAsync(ClaimsPrincipal principal, AnalyticsWidget item, CancellationToken cancellationToken)
    {
        var contextA = await permissionService.AuthorizeModuleAsync(principal, item.ModuleAId, ModuleAction.View, cancellationToken);
        var contextB = await permissionService.AuthorizeModuleAsync(principal, item.ModuleBId, ModuleAction.View, cancellationToken);
        return contextA is not null &&
            contextB is not null &&
            contextA.CanViewField(item.FieldAName) &&
            contextB.CanViewField(item.FieldBName);
    }

    private async Task<bool> CanManageAnalyticsAsync(ClaimsPrincipal principal, Guid moduleAId, Guid moduleBId, string fieldAName, string fieldBName, ModuleAction action, CancellationToken cancellationToken)
    {
        var contextA = await permissionService.AuthorizeModuleAsync(principal, moduleAId, action, cancellationToken);
        var contextB = await permissionService.AuthorizeModuleAsync(principal, moduleBId, action, cancellationToken);
        return contextA is not null &&
            contextB is not null &&
            contextA.CanViewField(fieldAName) &&
            contextB.CanViewField(fieldBName);
    }
}
