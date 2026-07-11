using System.Security.Claims;
using System.Text.Json;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class EntryService(
    IEntryRepository entryRepository,
    IModuleRepository moduleRepository,
    PermissionService permissionService) : IEntryService
{
    public async Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, Guid moduleId, CreateEntryRequestDto request, CancellationToken cancellationToken)
    {
        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, moduleId, ModuleAction.Create, cancellationToken);
        if (authorizationContext is null)
        {
            return await moduleRepository.ExistsAsync(moduleId, cancellationToken)
                ? ServiceResult<object>.Forbidden()
                : ServiceResult<object>.NotFound("Module not found.");
        }

        var fieldMap = PermissionService.BuildFieldMap(authorizationContext.Module.Fields);
        var validationError = ValidationHelpers.ValidateEntryData(request.Data, fieldMap);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var fieldPermissionError = permissionService.ValidateEditableFields(request.Data, authorizationContext);
        if (fieldPermissionError is not null)
        {
            return ServiceResult<object>.Forbidden();
        }

        var now = DateTime.UtcNow;
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            CreatedByUserId = authorizationContext.UserContext.User.Id,
            Timestamp = request.Timestamp?.ToUniversalTime() ?? now,
            Data = request.Data,
            CreatedAt = now,
            UpdatedAt = now
        };

        await entryRepository.AddAsync(entry, cancellationToken);
        await entryRepository.SaveChangesAsync(cancellationToken);
        await permissionService.EnsureEntryVisibilityDefaultsAsync(entry, request.VisibilityPermissions, cancellationToken);
        await entryRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<object>.Created(await ToResponseAsync(entry, authorizationContext, cancellationToken));
    }

    public async Task<ServiceResult<List<object>>> ListByModuleAsync(ClaimsPrincipal principal, Guid moduleId, CancellationToken cancellationToken)
    {
        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, moduleId, ModuleAction.View, cancellationToken);
        if (authorizationContext is null)
        {
            return await moduleRepository.ExistsAsync(moduleId, cancellationToken)
                ? ServiceResult<List<object>>.Forbidden()
                : ServiceResult<List<object>>.NotFound("Module not found.");
        }

        var entries = await entryRepository.ListByModuleAsync(moduleId, cancellationToken);
        var visibleEntries = await permissionService.FilterEntriesByVisibilityAsync(authorizationContext, entries, cancellationToken);

        var responses = new List<object>();
        foreach (var entry in visibleEntries)
        {
            responses.Add(await ToResponseAsync(entry, authorizationContext, cancellationToken));
        }

        return ServiceResult<List<object>>.Ok(responses);
    }

    public async Task<ServiceResult<object>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult<object>.NotFound("Entry not found.");
        }

        var accessContext = await permissionService.AuthorizeEntryAsync(principal, entry, ModuleAction.View, cancellationToken);
        if (accessContext is null)
        {
            return ServiceResult<object>.Forbidden();
        }

        return ServiceResult<object>.Ok(await ToResponseAsync(entry, accessContext.ModuleAuthorization, cancellationToken));
    }

    public async Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateEntryRequestDto request, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult<object>.NotFound("Entry not found.");
        }

        var accessContext = await permissionService.AuthorizeEntryAsync(principal, entry, ModuleAction.Edit, cancellationToken);
        if (accessContext is null)
        {
            return ServiceResult<object>.Forbidden();
        }

        var fieldPermissionError = permissionService.ValidateEditableFields(request.Data, accessContext.ModuleAuthorization);
        if (fieldPermissionError is not null)
        {
            return ServiceResult<object>.Forbidden();
        }

        var mergedData = MergeData(entry.Data, request.Data);
        var fieldMap = PermissionService.BuildFieldMap(accessContext.ModuleAuthorization.Module.Fields);
        var validationError = ValidationHelpers.ValidateEntryData(mergedData, fieldMap);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        entry.Timestamp = request.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow;
        entry.Data = mergedData;
        entry.UpdatedAt = DateTime.UtcNow;

        await entryRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.Ok(await ToResponseAsync(entry, accessContext.ModuleAuthorization, cancellationToken));
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult<object>.NotFound("Entry not found.");
        }

        var accessContext = await permissionService.AuthorizeEntryAsync(principal, entry, ModuleAction.Delete, cancellationToken);
        if (accessContext is null)
        {
            return ServiceResult<object>.Forbidden();
        }

        entryRepository.Remove(entry);
        await entryRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    public async Task<ServiceResult<object>> GetVisibilityAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult<object>.NotFound("Entry not found.");
        }

        var accessContext = await permissionService.AuthorizeEntryAsync(principal, entry, ModuleAction.View, cancellationToken);
        if (accessContext is null || !accessContext.CanManageVisibility)
        {
            return ServiceResult<object>.Forbidden();
        }

        return ServiceResult<object>.Ok(await permissionService.GetEntryVisibilityPermissionsAsync(id, cancellationToken));
    }

    public async Task<ServiceResult<object>> UpdateVisibilityAsync(ClaimsPrincipal principal, Guid id, UpdateEntryVisibilityRequestDto request, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return ServiceResult<object>.NotFound("Entry not found.");
        }

        var accessContext = await permissionService.AuthorizeEntryAsync(principal, entry, ModuleAction.View, cancellationToken);
        if (accessContext is null || !accessContext.CanManageVisibility)
        {
            return ServiceResult<object>.Forbidden();
        }

        await permissionService.ReplaceEntryVisibilityPermissionsAsync(entry, request.Permissions, cancellationToken);
        return ServiceResult<object>.Ok(await permissionService.GetEntryVisibilityPermissionsAsync(id, cancellationToken));
    }

    private async Task<object> ToResponseAsync(Entry entry, ModuleAuthorizationContext authorizationContext, CancellationToken cancellationToken)
    {
        var visibilityPermissions = await permissionService.GetEntryVisibilityPermissionsAsync(entry.Id, cancellationToken);

        return new
        {
            entry.Id,
            entry.ModuleId,
            entry.CreatedByUserId,
            entry.Timestamp,
            Data = permissionService.FilterEntryData(entry.Data, authorizationContext),
            entry.CreatedAt,
            entry.UpdatedAt,
            VisibilityPermissions = visibilityPermissions
        };
    }

    private static JsonElement MergeData(JsonElement existingData, JsonElement requestData)
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (existingData.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in existingData.EnumerateObject())
            {
                merged[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }
        }

        if (requestData.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in requestData.EnumerateObject())
            {
                merged[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }
        }

        return JsonSerializer.SerializeToElement(merged);
    }
}
