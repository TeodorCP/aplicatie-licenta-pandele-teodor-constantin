using System.Security.Claims;
using BusinessOps.Backend.Auth;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Seed;
using BusinessOps.Backend.Services.Interfaces;

namespace BusinessOps.Backend.Services.Implementations;

public class RoleService(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    PermissionService permissionService,
    IConfiguration configuration,
    PasswordHasherService passwordHasherService,
    ISeedRepository seedRepository) : IRoleService
{
    public async Task<ServiceResult<List<RoleDto>>> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<List<RoleDto>>.Forbidden();
        }

        var roles = await roleRepository.ListAsync(cancellationToken);
        return ServiceResult<List<RoleDto>>.Ok(roles.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<RoleDto>> CreateAsync(ClaimsPrincipal principal, CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<RoleDto>.Forbidden();
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<RoleDto>.BadRequest("Role name is required.");
        }

        if (await roleRepository.NameExistsAsync(name, null, cancellationToken))
        {
            return ServiceResult<RoleDto>.BadRequest("A role with this name already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        await roleRepository.AddAsync(role, cancellationToken);
        await roleRepository.SaveChangesAsync(cancellationToken);
        await seedRepository.EnsureSecurityDefaultsAsync(configuration, passwordHasherService);

        return ServiceResult<RoleDto>.Created(ToDto(role));
    }

    public async Task<ServiceResult<RoleDto>> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<RoleDto>.Forbidden();
        }

        var role = await roleRepository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return ServiceResult<RoleDto>.NotFound("Role not found.");
        }

        if (ModuleSeedService.IsSystemRoleName(role.Name))
        {
            return ServiceResult<RoleDto>.BadRequest("System roles cannot be renamed.");
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<RoleDto>.BadRequest("Role name is required.");
        }

        if (await roleRepository.NameExistsAsync(name, id, cancellationToken))
        {
            return ServiceResult<RoleDto>.BadRequest("A role with this name already exists.");
        }

        role.Name = name;
        await roleRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<RoleDto>.Ok(ToDto(role));
    }

    public async Task<ServiceResult<object>> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await permissionService.CanManageRolesAsync(principal, cancellationToken))
        {
            return ServiceResult<object>.Forbidden();
        }

        var role = await roleRepository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return ServiceResult<object>.NotFound("Role not found.");
        }

        if (ModuleSeedService.IsSystemRoleName(role.Name))
        {
            return ServiceResult<object>.BadRequest("System roles cannot be deleted.");
        }

        if (await userRepository.AnyInRoleAsync(id, cancellationToken))
        {
            return ServiceResult<object>.BadRequest("This role is assigned to existing users.");
        }

        roleRepository.Remove(role);
        await roleRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<object>.NoContent();
    }

    private static RoleDto ToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id.ToString(),
            Name = role.Name,
            IsSystemRole = ModuleSeedService.IsSystemRoleName(role.Name),
            CreatedAt = role.CreatedAt
        };
    }
}
