using Identity.Application.Abstractions;
using Shared.Domain.Primitives;

namespace Identity.Application.UseCases.RevokeToken;

/// <summary>Revoga um refresh token (logout). Preserva o RevokeToken do AuthController legado.</summary>
internal sealed class RevokeTokenHandler : ICommandHandler<RevokeTokenCommand>
{
    private readonly IUserRepository _repository;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RevokeTokenHandler(IUserRepository repository, ITokenService tokenService, TimeProvider timeProvider)
    {
        _repository = repository;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RevokeTokenCommand command, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var hash = _tokenService.HashRefreshToken(command.RefreshToken);

        var match = await _repository.GetByRefreshTokenHashAsync(hash, ct);
        if (match is null || !match.Value.Token.IsActiveAt(now))
            return Result.Failure(Error.NotFound("User.InvalidRefreshToken", "Refresh token inválido ou já revogado."));

        match.Value.Token.Revoke(replacedByTokenHash: null, revokedByIp: command.IpAddress, now);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}
