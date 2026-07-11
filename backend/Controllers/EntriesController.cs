using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
public class EntriesController(IEntryService entryService) : ControllerBase
{
    [HttpPost("api/modules/{moduleId:guid}/entries")]
    public async Task<IActionResult> CreateEntry([FromRoute] Guid moduleId, [FromBody] CreateEntryRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.CreateAsync(User, moduleId, request, cancellationToken));
    }

    [HttpGet("api/modules/{moduleId:guid}/entries")]
    public async Task<IActionResult> GetEntriesByModule([FromRoute] Guid moduleId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.ListByModuleAsync(User, moduleId, cancellationToken));
    }

    [HttpGet("api/entries/{id:guid}")]
    public async Task<IActionResult> GetEntryById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.GetAsync(User, id, cancellationToken));
    }

    [HttpPut("api/entries/{id:guid}")]
    public async Task<IActionResult> UpdateEntry([FromRoute] Guid id, [FromBody] UpdateEntryRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("api/entries/{id:guid}")]
    public async Task<IActionResult> DeleteEntry([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.DeleteAsync(User, id, cancellationToken));
    }

    [HttpGet("api/entries/{id:guid}/visibility")]
    public async Task<IActionResult> GetVisibility([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.GetVisibilityAsync(User, id, cancellationToken));
    }

    [HttpPut("api/entries/{id:guid}/visibility")]
    public async Task<IActionResult> UpdateVisibility([FromRoute] Guid id, [FromBody] UpdateEntryVisibilityRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await entryService.UpdateVisibilityAsync(User, id, request, cancellationToken));
    }
}
