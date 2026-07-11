using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/modules")]
public class ModulesController(IModuleService moduleService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await moduleService.CreateAsync(User, request, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetModules(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await moduleService.ListAsync(User, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetModuleById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await moduleService.GetAsync(User, id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateModule([FromRoute] Guid id, [FromBody] UpdateModuleRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await moduleService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteModule([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await moduleService.DeleteAsync(User, id, cancellationToken));
    }
}
