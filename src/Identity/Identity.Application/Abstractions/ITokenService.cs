using Identity.Domain.Aggregates;

namespace Identity.Application.Abstractions;

/// <summary>
/// Bundle de tokens: JWT de acesso (claims sub/email/unique_name/jti/security_stamp preservados do
/// GearFlow) + refresh token (raw entregue ao cliente, hash persistido).
/// </summary>
public sealed record TokenBundle(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshTokenRaw,
    string RefreshTokenHash,
    DateTime RefreshTokenExpiresAtUtc);

public interface ITokenService
{
    TokenBundle Generate(User user);
    string HashRefreshToken(string rawToken);
}
