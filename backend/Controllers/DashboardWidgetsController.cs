using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
public class DashboardWidgetsController(IDashboardWidgetService dashboardWidgetService) : ControllerBase
{
    [HttpGet("api/dashboard/widgets")]
    public async Task<IActionResult> GetWidgets(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await dashboardWidgetService.ListAsync(User, cancellationToken));
    }

    [HttpPost("api/dashboard/widgets")]
    public async Task<IActionResult> CreateWidget([FromBody] CreateDashboardWidgetRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await dashboardWidgetService.CreateAsync(User, request, cancellationToken));
    }

    [HttpPut("api/dashboard/widgets/{id:guid}")]
    public async Task<IActionResult> UpdateWidget([FromRoute] Guid id, [FromBody] UpdateDashboardWidgetRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await dashboardWidgetService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("api/dashboard/widgets/{id:guid}")]
    public async Task<IActionResult> DeleteWidget([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await dashboardWidgetService.DeleteAsync(User, id, cancellationToken));
    }
}
