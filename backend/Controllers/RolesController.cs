using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await roleService.ListAsync(User, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await roleService.CreateAsync(User, request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await roleService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await roleService.DeleteAsync(User, id, cancellationToken));
    }
}
