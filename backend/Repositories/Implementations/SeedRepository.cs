using BusinessOps.Backend.Auth;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Seed;

namespace BusinessOps.Backend.Repositories.Implementations;

public class SeedRepository(AppDbContext dbContext) : ISeedRepository
{
    public Task EnsurePermissionsForModuleAsync(Module module)
    {
        return ModuleSeedService.EnsurePermissionsForModuleAsync(dbContext, module);
    }

    public Task EnsureSecurityDefaultsAsync(IConfiguration configuration, PasswordHasherService passwordHasherService)
    {
        return ModuleSeedService.EnsureSecurityDefaultsAsync(dbContext, configuration, passwordHasherService);
    }
}
