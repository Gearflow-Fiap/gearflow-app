using Identity.Application.Abstractions;
using Identity.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;

    public UserRepository(IdentityDbContext db) => _db = db;

    public async Task AddAsync(User user, CancellationToken ct = default) => await _db.Users.AddAsync(user, ct);

    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<User?> GetByNormalizedLoginAsync(string normalizedLogin, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedLogin || u.NormalizedUserName == normalizedLogin, ct);

    public async Task<(User User, RefreshToken Token)?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
        if (token is null) return null;

        var user = await _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        return user is null ? null : (user, token);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
