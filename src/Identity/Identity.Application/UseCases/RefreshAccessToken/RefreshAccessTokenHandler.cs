using Identity.Application.Abstractions;
using Identity.Application.DTOs;
using Shared.Domain.Primitives;

namespace Identity.Application.UseCases.RefreshAccessToken;

/// <summary>Rotaciona o refresh token: revoga o antigo (apontando o substituto) e emite um novo par.</summary>
internal sealed class RefreshAccessTokenHandler : ICommandHandler<RefreshAccessTokenCommand, AuthTokenResponse>
{
    private readonly IUserRepository _repository;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshAccessTokenHandler(IUserRepository repository, ITokenService tokenService, TimeProvider timeProvider)
    {
        _repository = repository;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthTokenResponse>> Handle(RefreshAccessTokenCommand command, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var hash = _tokenService.HashRefreshToken(command.RefreshToken);

        var match = await _repository.GetByRefreshTokenHashAsync(hash, ct);
        if (match is null || !match.Value.Token.IsActiveAt(now))
            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("User.InvalidRefreshToken", "Refresh token inválido ou expirado."));

        var (user, oldToken) = match.Value;
        if (!user.CanAuthenticateAt(now))
            return Result.Failure<AuthTokenResponse>(Error.Unauthorized("User.LockedOrInactive", "Conta bloqueada ou inativa."));

        var bundle = _tokenService.Generate(user);
        oldToken.Revoke(bundle.RefreshTokenHash, command.IpAddress, now);
        var token = user.IssueRefreshToken(bundle.RefreshTokenHash, bundle.RefreshTokenExpiresAtUtc, command.IpAddress, now);
        _repository.AddRefreshToken(token);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(new AuthTokenResponse(bundle.AccessToken, bundle.RefreshTokenRaw, bundle.AccessTokenExpiresAtUtc));
    }
}
