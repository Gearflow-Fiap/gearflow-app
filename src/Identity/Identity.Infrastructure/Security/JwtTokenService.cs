using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Domain.Aggregates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Gera o JWT de acesso com os claims preservados do GearFlow (NameIdentifier, sub, email,
/// unique_name, jti, security_stamp) e o refresh token (hash SHA-256). Assina com o segredo
/// compartilhado do <see cref="JwtOptions"/> (o mesmo que a Lambda de CPF usa).
/// </summary>
internal sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenDays = 7;

    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public TokenBundle Generate(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var accessExpires = now.AddSeconds(_options.ExpiresInSeconds);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("security_stamp", user.SecurityStamp),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var rawRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenBundle(
            accessToken, accessExpires, rawRefresh, HashRefreshToken(rawRefresh), now.AddDays(RefreshTokenDays));
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
