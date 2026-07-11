using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;
using BusinessOps.Backend.Repositories.Interfaces;

namespace BusinessOps.Backend.Repositories.Implementations;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<List<UserSummaryDto>> GetSummariesAsync(Expression<Func<User, UserSummaryDto>> selector, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public Task<UserSummaryDto?> GetSummaryAsync(Guid id, Expression<Func<User, UserSummaryDto>> selector, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.Id == id)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? userIdToExclude, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            x => x.Email == email && (!userIdToExclude.HasValue || x.Id != userIdToExclude.Value),
            cancellationToken);
    }

    public Task<bool> AnyInRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.RoleId == roleId, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Remove(User user)
    {
        dbContext.Users.Remove(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
