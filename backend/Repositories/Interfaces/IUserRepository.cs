using System.Linq.Expressions;
using BusinessOps.Backend.DTOs;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<UserSummaryDto>> GetSummariesAsync(Expression<Func<User, UserSummaryDto>> selector, CancellationToken cancellationToken);

    Task<UserSummaryDto?> GetSummaryAsync(Guid id, Expression<Func<User, UserSummaryDto>> selector, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? userIdToExclude, CancellationToken cancellationToken);

    Task<bool> AnyInRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    void Remove(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
