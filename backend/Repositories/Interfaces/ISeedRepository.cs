using BusinessOps.Backend.Auth;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface ISeedRepository
{
    Task EnsurePermissionsForModuleAsync(Module module);

    Task EnsureSecurityDefaultsAsync(IConfiguration configuration, PasswordHasherService passwordHasherService);
}
