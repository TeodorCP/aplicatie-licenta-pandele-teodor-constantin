using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Helpers;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.ListAsync(User, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.CreateAsync(User, request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.GetAsync(User, id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.UpdateAsync(User, id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.DeleteAsync(User, id, cancellationToken));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        return this.ToActionResult(await userService.ResetPasswordAsync(User, id, request, cancellationToken));
    }
}
