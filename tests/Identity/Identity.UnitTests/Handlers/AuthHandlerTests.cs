using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.RefreshAccessToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.RevokeToken;
using Identity.Domain.Aggregates;
using NSubstitute;
using Shared.Domain.Primitives;

namespace Identity.UnitTests.Handlers;

public sealed class AuthHandlerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();

    private static User AStaff()
    {
        var user = User.Create("staff@gearflow.com", "staff", Now).Value;
        user.SetPasswordHash("stored-hash", Now);
        return user;
    }

    [Fact]
    public async Task Register_rejects_weak_password()
    {
        var handler = new RegisterUserHandler(_users, _hasher, TimeProvider.System);
        var result = await handler.Handle(new RegisterUserCommand("a@b.com", "user", "123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.WeakPassword");
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        _users.ExistsByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RegisterUserHandler(_users, _hasher, TimeProvider.System);

        var result = await handler.Handle(new RegisterUserCommand("a@b.com", "user", "Secret123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
    }

    [Fact]
    public async Task Register_success_hashes_and_persists()
    {
        _users.ExistsByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _hasher.Hash("Secret123").Returns("hashed");
        var handler = new RegisterUserHandler(_users, _hasher, TimeProvider.System);

        var result = await handler.Handle(new RegisterUserCommand("a@b.com", "user", "Secret123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_with_wrong_password_registers_failure_and_returns_unauthorized()
    {
        var user = AStaff();
        _users.GetByNormalizedLoginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = new LoginUserHandler(_users, _hasher, _tokens, TimeProvider.System);
        var result = await handler.Handle(new LoginUserCommand("staff@gearflow.com", "wrong", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        user.AccessFailedCount.Should().Be(1);
        await _users.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_success_returns_tokens()
    {
        var user = AStaff();
        _users.GetByNormalizedLoginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), "Secret123").Returns(true);
        _tokens.Generate(user).Returns(new TokenBundle("access-jwt", Now.AddHours(1), "raw", "hash", Now.AddDays(7)));

        var handler = new LoginUserHandler(_users, _hasher, _tokens, TimeProvider.System);
        var result = await handler.Handle(new LoginUserCommand("staff@gearflow.com", "Secret123", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-jwt");
        user.RefreshTokens.Should().ContainSingle();
    }

    [Fact]
    public async Task Login_unknown_user_is_unauthorized()
    {
        _users.GetByNormalizedLoginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new LoginUserHandler(_users, _hasher, _tokens, TimeProvider.System);

        var result = await handler.Handle(new LoginUserCommand("ghost@x.com", "x", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task RevokeToken_returns_notfound_when_absent()
    {
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("hash");
        _users.GetByRefreshTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(((User, RefreshToken)?)null);

        var handler = new RevokeTokenHandler(_users, _tokens, TimeProvider.System);
        var result = await handler.Handle(new RevokeTokenCommand("raw", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidRefreshToken");
    }

    private static (User user, RefreshToken token) StaffWithActiveRefresh()
    {
        var user = AStaff();
        var token = user.IssueRefreshToken("old-hash", Now.AddDays(7), "127.0.0.1", Now);
        return (user, token);
    }

    [Fact]
    public async Task Refresh_with_active_token_rotates_and_returns_new_tokens()
    {
        var (user, oldToken) = StaffWithActiveRefresh();
        _tokens.HashRefreshToken("raw").Returns("lookup-hash");
        _users.GetByRefreshTokenHashAsync("lookup-hash", Arg.Any<CancellationToken>()).Returns((user, oldToken));
        _tokens.Generate(user).Returns(new TokenBundle("new-access", Now.AddHours(1), "new-raw", "new-hash", Now.AddDays(7)));

        var handler = new RefreshAccessTokenHandler(_users, _tokens, TimeProvider.System);
        var result = await handler.Handle(new RefreshAccessTokenCommand("raw", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access");
        result.Value.RefreshToken.Should().Be("new-raw");
        oldToken.IsActiveAt(Now).Should().BeFalse();            // token antigo revogado (rotação)
        oldToken.ReplacedByTokenHash.Should().Be("new-hash");   // aponta o substituto
        _users.Received(1).AddRefreshToken(Arg.Any<RefreshToken>());
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_with_unknown_token_is_unauthorized()
    {
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("h");
        _users.GetByRefreshTokenHashAsync("h", Arg.Any<CancellationToken>()).Returns(((User, RefreshToken)?)null);

        var handler = new RefreshAccessTokenHandler(_users, _tokens, TimeProvider.System);
        var result = await handler.Handle(new RefreshAccessTokenCommand("raw", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidRefreshToken");
    }

    [Fact]
    public async Task Refresh_with_revoked_token_is_unauthorized()
    {
        var (user, token) = StaffWithActiveRefresh();
        token.Revoke("outro", null, Now); // já revogado → IsActiveAt false
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("h");
        _users.GetByRefreshTokenHashAsync("h", Arg.Any<CancellationToken>()).Returns((user, token));

        var handler = new RefreshAccessTokenHandler(_users, _tokens, TimeProvider.System);
        var result = await handler.Handle(new RefreshAccessTokenCommand("raw", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidRefreshToken");
    }

    [Fact]
    public async Task Refresh_when_account_inactive_is_unauthorized()
    {
        var (user, token) = StaffWithActiveRefresh();
        user.Deactivate(Now); // conta inativa
        _tokens.HashRefreshToken(Arg.Any<string>()).Returns("h");
        _users.GetByRefreshTokenHashAsync("h", Arg.Any<CancellationToken>()).Returns((user, token));

        var handler = new RefreshAccessTokenHandler(_users, _tokens, TimeProvider.System);
        var result = await handler.Handle(new RefreshAccessTokenCommand("raw", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.LockedOrInactive");
    }
}
