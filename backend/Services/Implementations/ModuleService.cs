using System.Security.Claims;
using System.Text.Json;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Seed;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class ModuleService(
    IModuleRepository moduleRepository,
    PermissionService permissionService,
    ISeedRepository seedRepository) : IModuleService
{
    public async Task<ServiceResult<object>> CreateAsync(ClaimsPrincipal principal, CreateModuleRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var validationError = ValidationHelpers.ValidateModuleRequest(request);
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var requestedName = request.Name.Trim();
        if (await moduleRepository.NameExistsAsync(requestedName, null, cancellationToken))
        {
            return ServiceResult<object>.BadRequest("A module with this name already exists.");
        }

        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = requestedName,
            Fields = SerializeFields(request.Fields),
            CreatedAt = DateTime.UtcNow
        };

        await moduleRepository.AddAsync(module, cancellationToken);
        await moduleRepository.SaveChangesAsync(cancellationToken);
        await seedRepository.EnsurePermissionsForModuleAsync(module);

        return ServiceResult<object>.Created(ToResponse(module, module.Fields));
    }

    public async Task<ServiceResult<List<object>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var modules = await permissionService.GetViewableModulesAsync(principal, cancellationToken);
        var responses = new List<object>();

        foreach (var module in modules)
        {
            var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, module.Id, ModuleAction.View, cancellationToken);
            if (authorizationContext is null)
            {
                continue;
            }

            responses.Add(ToResponse(module, permissionService.FilterModuleFields(module.Fields, authorizationContext)));
        }

        return ServiceResult<List<object>>.Ok(responses);
    }

    public async Task<ServiceResult<object>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, id, ModuleAction.View, cancellationToken);
        if (authorizationContext is null)
        {
            return await moduleRepository.ExistsAsync(id, cancellationToken)
                ? ServiceResult<object>.Forbidden()
                : ServiceResult<object>.NotFound("Module not found.");
        }

        return ServiceResult<object>.Ok(ToResponse(
            authorizationContext.Module,
            permissionService.FilterModuleFields(authorizationContext.Module.Fields, authorizationContext)));
    }

    public async Task<ServiceResult<object>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateModuleRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var module = await moduleRepository.GetByIdAsync(id, cancellationToken);
        if (module is null)
        {
            return ServiceResult<object>.NotFound("Module not found.");
        }

        var validationError = ValidationHelpers.ValidateModuleRequest(new CreateModuleRequestDto
        {
            Name = request.Name,
            Fields = request.Fields
        });
        if (validationError is not null)
        {
            return ServiceResult<object>.BadRequest(validationError);
        }

        var requestedName = request.Name.Trim();
        if (await moduleRepository.NameExistsAsync(requestedName, id, cancellationToken))
        {
            return ServiceResult<object>.BadRequest("A module with this name already exists.");
        }

        module.Name = requestedName;
        module.Fields = SerializeFields(request.Fields);

        await moduleRepository.SaveChangesAsync(cancellationToken);
        await seedRepository.EnsurePermissionsForModuleAsync(module);

        var authorizationContext = await permissionService.AuthorizeModuleAsync(principal, id, ModuleAction.View, cancellationToken);
        var filteredFields = authorizationContext is null
            ? module.Fields
            : permissionService.FilterModuleFields(module.Fields, authorizationContext);

        return ServiceResult<object>.Ok(ToResponse(module, filteredFields));
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var module = await moduleRepository.GetByIdAsync(id, cancellationToken);
        if (module is null)
        {
            return ServiceResult<object>.NotFound("Module not found.");
        }

        moduleRepository.Remove(module);
        await moduleRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    private static object ToResponse(Module module, JsonElement fields)
    {
        return new
        {
            module.Id,
            module.Name,
            Category = ModuleSeedService.GetModuleCategory(module.Name),
            Fields = fields,
            module.CreatedAt
        };
    }

    private static JsonElement SerializeFields(IEnumerable<ModuleFieldDto> fields)
    {
        return JsonSerializer.SerializeToElement(fields.Select(field => new
        {
            name = field.Name.Trim(),
            type = FieldTypeConstants.NormalizeFieldType(field.Type)
        }));
    }
}
