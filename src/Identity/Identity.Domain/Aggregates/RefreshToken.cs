using Identity.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Identity.Domain.Aggregates;

/// <summary>Refresh token (hash SHA-256), rotacionável e rastreado por IP. Preservado do GearFlow.</summary>
public sealed class RefreshToken : Entity<Guid>
{
    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    internal RefreshToken(UserId userId, string tokenHash, DateTime expiresAt, string? createdByIp, DateTime nowUtc)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
        CreatedOn = nowUtc;
    }

    private RefreshToken() : base(Guid.NewGuid())
    {
        UserId = null!;
        TokenHash = null!;
    }

    public bool IsActiveAt(DateTime nowUtc) => !RevokedAt.HasValue && ExpiresAt > nowUtc;

    public void Revoke(string? replacedByTokenHash, string? revokedByIp, DateTime nowUtc)
    {
        RevokedAt = nowUtc;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
        UpdatedOn = nowUtc;
    }
}
