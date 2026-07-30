using Identity.Application.Abstractions;
using Identity.Application.DTOs;
using Identity.Domain.Aggregates;
using Shared.Domain.Primitives;

namespace Identity.Application.UseCases.LoginUser;

/// <summary>
/// Login por e-mail/usuário + senha. Preserva o GearFlow: normaliza, respeita lockout, incrementa
/// falhas (bloqueia após N), reseta na entrada bem-sucedida e emite o par access/refresh.
/// </summary>
internal sealed class LoginUserHandler : ICommandHandler<LoginUserCommand, AuthTokenResponse>
{
    private const int MaxAccessFailedCount = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public LoginUserHandler(
        IUserRepository repository, IPasswordHasher passwordHasher, ITokenService tokenService, TimeProvider timeProvider)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthTokenResponse>> Handle(LoginUserCommand command, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var normalized = User.Normalize(command.EmailOrUserName.Trim());

        var user = await _repository.GetByNormalizedLoginAsync(normalized, ct);
        if (user is null)
            return InvalidCredentials();

        if (!user.CanAuthenticateAt(now))
            return Result.Failure<AuthTokenResponse>(
                Error.Unauthorized("User.LockedOrInactive", "Conta bloqueada ou inativa. Tente mais tarde."));

        if (!_passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            user.RegisterFailedAccess(MaxAccessFailedCount, LockoutMinutes, now);
            await _repository.SaveChangesAsync(ct);
            return InvalidCredentials();
        }

        user.ResetAccessFailures(now);

        var bundle = _tokenService.Generate(user);
        user.IssueRefreshToken(bundle.RefreshTokenHash, bundle.RefreshTokenExpiresAtUtc, command.IpAddress, now);
        await _repository.SaveChangesAsync(ct);

        return Result.Success(new AuthTokenResponse(bundle.AccessToken, bundle.RefreshTokenRaw, bundle.AccessTokenExpiresAtUtc));
    }

    private static Result<AuthTokenResponse> InvalidCredentials() =>
        Result.Failure<AuthTokenResponse>(Error.Unauthorized("User.InvalidCredentials", "Credenciais inválidas."));
}
