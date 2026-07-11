using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<AuthUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
