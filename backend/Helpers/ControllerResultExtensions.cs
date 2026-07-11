using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.Services;

namespace BusinessOps.Backend.Helpers;

public static class ControllerResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Ok => controller.Ok(result.Value),
            ServiceStatus.Created => controller.StatusCode(StatusCodes.Status201Created, result.Value),
            ServiceStatus.NoContent => controller.NoContent(),
            ServiceStatus.BadRequest => controller.BadRequest(new { error = result.Error }),
            ServiceStatus.NotFound => controller.NotFound(new { error = result.Error }),
            ServiceStatus.Forbidden => controller.Forbid(),
            ServiceStatus.Unauthorized => controller.Unauthorized(new { error = result.Error }),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected service result." })
        };
    }
}
