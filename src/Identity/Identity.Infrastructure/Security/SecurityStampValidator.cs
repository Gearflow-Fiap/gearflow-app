using Identity.Application.Abstractions;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Security;

internal sealed class SecurityStampValidator : ISecurityStampValidator
{
    private readonly IdentityDbContext _db;

    public SecurityStampValidator(IdentityDbContext db) => _db = db;

    public async Task<bool> IsCurrentAsync(Guid userId, string securityStamp, CancellationToken ct = default)
    {
        var id = UserId.From(userId);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is not null && user.IsActive && user.SecurityStamp == securityStamp;
    }
}
