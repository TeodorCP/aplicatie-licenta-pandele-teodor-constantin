using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
public class PermissionsController(IPermissionManagementService permissionManagementService) : ControllerBase
{
    [HttpGet("api/modules/{moduleId:guid}/permissions")]
    public async Task<IActionResult> GetModulePermissions([FromRoute] Guid moduleId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await permissionManagementService.GetModulePermissionsAsync(User, moduleId, cancellationToken));
    }

    [HttpPut("api/modules/{moduleId:guid}/permissions")]
    public async Task<IActionResult> UpdateModulePermissions([FromRoute] Guid moduleId, [FromBody] UpdateModulePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await permissionManagementService.UpdateModulePermissionsAsync(User, moduleId, request, cancellationToken));
    }

    [HttpGet("api/modules/{moduleId:guid}/field-permissions")]
    public async Task<IActionResult> GetFieldPermissions([FromRoute] Guid moduleId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await permissionManagementService.GetFieldPermissionsAsync(User, moduleId, cancellationToken));
    }

    [HttpPut("api/modules/{moduleId:guid}/field-permissions")]
    public async Task<IActionResult> UpdateFieldPermissions([FromRoute] Guid moduleId, [FromBody] UpdateFieldPermissionsRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await permissionManagementService.UpdateFieldPermissionsAsync(User, moduleId, request, cancellationToken));
    }
}
