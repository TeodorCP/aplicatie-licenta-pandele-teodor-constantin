using System.Linq.Expressions;
using System.Security.Claims;
using BusinessOps.Backend.Auth;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    PermissionService permissionService,
    PasswordHasherService passwordHasherService) : IUserService
{
    public async Task<ServiceResult<List<UserSummaryDto>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<List<UserSummaryDto>>.Forbidden();
        }

        return ServiceResult<List<UserSummaryDto>>.Ok(await userRepository.GetSummariesAsync(ToSummaryDto(), cancellationToken));
    }

    public async Task<ServiceResult<UserSummaryDto>> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<UserSummaryDto>.Forbidden();
        }

        var user = await userRepository.GetSummaryAsync(id, ToSummaryDto(), cancellationToken);
        return user is null
            ? ServiceResult<UserSummaryDto>.NotFound("User not found.")
            : ServiceResult<UserSummaryDto>.Ok(user);
    }

    public async Task<ServiceResult<UserSummaryDto>> CreateAsync(ClaimsPrincipal principal, CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<UserSummaryDto>.Forbidden();
        }

        var validationError = await ValidateUserRequestAsync(request.Email, request.FullName, request.RoleId, request.Password, null, cancellationToken);
        if (validationError is not null)
        {
            return ServiceResult<UserSummaryDto>.BadRequest(validationError);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasherService.HashPassword(request.Password),
            RoleId = Guid.Parse(request.RoleId),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<UserSummaryDto>.Created((await userRepository.GetSummaryAsync(user.Id, ToSummaryDto(), cancellationToken))!);
    }

    public async Task<ServiceResult<UserSummaryDto>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<UserSummaryDto>.Forbidden();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserSummaryDto>.NotFound("User not found.");
        }

        var validationError = await ValidateUserRequestAsync(request.Email, request.FullName, request.RoleId, null, id, cancellationToken);
        if (validationError is not null)
        {
            return ServiceResult<UserSummaryDto>.BadRequest(validationError);
        }

        user.Email = request.Email.Trim().ToLowerInvariant();
        user.FullName = request.FullName.Trim();
        user.RoleId = Guid.Parse(request.RoleId);
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<UserSummaryDto>.Ok((await userRepository.GetSummaryAsync(id, ToSummaryDto(), cancellationToken))!);
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<object>.NotFound("User not found.");
        }

        userRepository.Remove(user);
        await userRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    public async Task<ServiceResult<object>> ResetPasswordAsync(ClaimsPrincipal principal, Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return ServiceResult<object>.NotFound("User not found.");
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.TemporaryPassword)
            ? PasswordGenerator.GenerateTemporaryPassword()
            : request.TemporaryPassword.Trim();

        if (temporaryPassword.Length < 6)
        {
            return ServiceResult<object>.BadRequest("Temporary password must be at least 6 characters long.");
        }

        user.PasswordHash = passwordHasherService.HashPassword(temporaryPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<object>.Ok(new { temporaryPassword });
    }

    private async Task<string?> ValidateUserRequestAsync(string email, string fullName, string roleId, string? password, Guid? userIdToExclude, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Full name is required.";
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (!Guid.TryParse(roleId, out var parsedRoleId))
        {
            return "RoleId is invalid.";
        }

        if (password is not null && (string.IsNullOrWhiteSpace(password) || password.Length < 6))
        {
            return "Password must be at least 6 characters long.";
        }

        if (!await roleRepository.ExistsAsync(parsedRoleId, cancellationToken))
        {
            return "Role not found.";
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await userRepository.EmailExistsAsync(normalizedEmail, userIdToExclude, cancellationToken))
        {
            return "A user with this email already exists.";
        }

        return null;
    }

    private static Expression<Func<User, UserSummaryDto>> ToSummaryDto()
    {
        return x => new UserSummaryDto
        {
            Id = x.Id.ToString(),
            Email = x.Email,
            FullName = x.FullName,
            RoleId = x.RoleId.ToString(),
            RoleName = x.Role.Name,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
