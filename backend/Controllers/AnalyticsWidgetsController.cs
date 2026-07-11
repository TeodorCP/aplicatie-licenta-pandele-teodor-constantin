using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
public class AnalyticsWidgetsController(IAnalyticsWidgetService analyticsWidgetService) : ControllerBase
{
    [HttpGet("api/analytics/widgets")]
    public async Task<IActionResult> GetWidgets(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await analyticsWidgetService.ListAsync(User, cancellationToken));
    }

    [HttpPost("api/analytics/widgets")]
    public async Task<IActionResult> CreateWidget([FromBody] CreateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await analyticsWidgetService.CreateAsync(User, request, cancellationToken));
    }

    [HttpPut("api/analytics/widgets/{id:guid}")]
    public async Task<IActionResult> UpdateWidget([FromRoute] Guid id, [FromBody] UpdateAnalyticsWidgetRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await analyticsWidgetService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("api/analytics/widgets/{id:guid}")]
    public async Task<IActionResult> DeleteWidget([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await analyticsWidgetService.DeleteAsync(User, id, cancellationToken));
    }
}
