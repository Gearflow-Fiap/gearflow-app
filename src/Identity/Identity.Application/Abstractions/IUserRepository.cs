using Identity.Domain.Aggregates;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default);
    Task<User?> GetByNormalizedLoginAsync(string normalizedLogin, CancellationToken ct = default);
    Task<(User User, RefreshToken Token)?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
