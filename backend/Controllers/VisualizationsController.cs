using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
public class VisualizationsController(IVisualizationService visualizationService) : ControllerBase
{
    [HttpPost("api/visualizations")]
    public async Task<IActionResult> CreateVisualization([FromBody] CreateVisualizationRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.CreateAsync(User, request, cancellationToken));
    }

    [HttpGet("api/visualizations")]
    public async Task<IActionResult> GetVisualizations(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.ListAsync(User, cancellationToken));
    }

    [HttpGet("api/modules/{moduleId:guid}/visualizations")]
    public async Task<IActionResult> GetVisualizationsByModule([FromRoute] Guid moduleId, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.ListByModuleAsync(User, moduleId, cancellationToken));
    }

    [HttpGet("api/visualizations/{id:guid}")]
    public async Task<IActionResult> GetVisualizationById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.GetAsync(User, id, cancellationToken));
    }

    [HttpPut("api/visualizations/{id:guid}")]
    public async Task<IActionResult> UpdateVisualization([FromRoute] Guid id, [FromBody] UpdateVisualizationRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("api/visualizations/{id:guid}")]
    public async Task<IActionResult> DeleteVisualization([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await visualizationService.DeleteAsync(User, id, cancellationToken));
    }
}
