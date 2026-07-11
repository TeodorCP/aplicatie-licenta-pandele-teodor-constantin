using System.Security.Claims;
using BusinessOps.Backend.Auth;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class AuthService(
    IUserRepository userRepository,
    PasswordHasherService passwordHasherService,
    JwtTokenService jwtTokenService,
    PermissionService permissionService) : IAuthService
{
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailWithRoleAsync(email, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasherService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthResponseDto>.Unauthorized("Invalid email or password.");
        }

        var modulePermissions = await permissionService.GetEffectiveModulePermissionsForRoleAsync(user.RoleId, cancellationToken);
        var fieldPermissions = await permissionService.GetEffectiveFieldPermissionsForRoleAsync(user.RoleId, cancellationToken);
        var (token, expiresAt) = jwtTokenService.CreateToken(user, user.Role.Name);

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = permissionService.BuildAuthUserDto(user, user.Role.Name, modulePermissions, fieldPermissions)
        });
    }

    public async Task<ServiceResult<AuthUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var context = await permissionService.GetCurrentUserContextAsync(principal, cancellationToken);
        if (context is null || !context.User.IsActive)
        {
            return ServiceResult<AuthUserDto>.Unauthorized("User not found.");
        }

        var modulePermissions = await permissionService.GetEffectiveModulePermissionsForRoleAsync(context.Role.Id, cancellationToken);
        var fieldPermissions = await permissionService.GetEffectiveFieldPermissionsForRoleAsync(context.Role.Id, cancellationToken);

        return ServiceResult<AuthUserDto>.Ok(permissionService.BuildAuthUserDto(
            context.User,
            context.Role.Name,
            modulePermissions,
            fieldPermissions));
    }
}
