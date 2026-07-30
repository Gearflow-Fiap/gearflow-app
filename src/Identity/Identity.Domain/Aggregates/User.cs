using Identity.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Identity.Domain.Aggregates;

/// <summary>
/// Identidade de funcionário (staff) da oficina. Preserva do GearFlow: normalização de email/username,
/// lockout por tentativas, e rotação de <see cref="SecurityStamp"/> (invalida JWTs ao trocar a senha).
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private readonly List<RefreshToken> _refreshTokens = new();

    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string UserName { get; private set; }
    public string NormalizedUserName { get; private set; }
    public string PasswordHash { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User(UserId id, string email, string userName, DateTime nowUtc) : base(id)
    {
        Email = email;
        NormalizedEmail = Normalize(email);
        UserName = userName;
        NormalizedUserName = Normalize(userName);
        PasswordHash = string.Empty;
        EmailConfirmed = false;
        IsActive = true;
        AccessFailedCount = 0;
        SecurityStamp = Guid.NewGuid().ToString("N");
        CreatedOn = nowUtc;
    }

    private User() : base(UserId.New())
    {
        Email = null!;
        NormalizedEmail = null!;
        UserName = null!;
        NormalizedUserName = null!;
        PasswordHash = null!;
        SecurityStamp = null!;
    }

    public static Result<User> Create(string email, string userName, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<User>(Error.Validation("User.InvalidEmail", "E-mail inválido.", "email"));
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure<User>(Error.Validation("User.UserNameRequired", "Nome de usuário é obrigatório.", "userName"));

        return Result.Success(new User(UserId.New(), email.Trim(), userName.Trim(), nowUtc));
    }

    public bool CanAuthenticateAt(DateTime nowUtc) =>
        IsActive && (!LockoutEnd.HasValue || LockoutEnd.Value <= nowUtc);

    public void SetPasswordHash(string passwordHash, DateTime nowUtc)
    {
        PasswordHash = passwordHash;
        UpdateSecurityStamp(nowUtc);
    }

    public void ConfirmEmail(DateTime nowUtc) { EmailConfirmed = true; UpdatedOn = nowUtc; }
    public void Activate(DateTime nowUtc) { IsActive = true; UpdatedOn = nowUtc; }
    public void Deactivate(DateTime nowUtc) { IsActive = false; UpdatedOn = nowUtc; }

    public void RegisterFailedAccess(int maxAccessFailedCount, int lockoutMinutes, DateTime nowUtc)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxAccessFailedCount)
        {
            AccessFailedCount = 0;
            LockoutEnd = nowUtc.AddMinutes(lockoutMinutes);
        }
        UpdatedOn = nowUtc;
    }

    public void ResetAccessFailures(DateTime nowUtc)
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        UpdatedOn = nowUtc;
    }

    public void UpdateSecurityStamp(DateTime nowUtc)
    {
        SecurityStamp = Guid.NewGuid().ToString("N");
        UpdatedOn = nowUtc;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAt, string? createdByIp, DateTime nowUtc)
    {
        var token = new RefreshToken(Id, tokenHash, expiresAt, createdByIp, nowUtc);
        _refreshTokens.Add(token);
        return token;
    }

    public static string Normalize(string value) => value.ToUpperInvariant();
}
