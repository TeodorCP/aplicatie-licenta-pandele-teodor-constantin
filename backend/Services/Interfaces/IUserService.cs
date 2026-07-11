using System.Security.Claims;
using BusinessOps.Backend.DTOs;

namespace BusinessOps.Backend.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResult<List<UserSummaryDto>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<ServiceResult<UserSummaryDto>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<UserSummaryDto>> CreateAsync(ClaimsPrincipal principal, CreateUserRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<UserSummaryDto>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateUserRequestDto request, CancellationToken cancellationToken);

    Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);

    Task<ServiceResult<object>> ResetPasswordAsync(ClaimsPrincipal principal, Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken);
}
