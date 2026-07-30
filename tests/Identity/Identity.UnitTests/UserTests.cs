using FluentAssertions;
using Identity.Domain.Aggregates;

namespace Identity.UnitTests;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    private static User NewUser() => User.Create("staff@gearflow.com", "staff", Now).Value;

    [Fact]
    public void Create_normalizes_email_and_username()
    {
        var user = NewUser();

        user.NormalizedEmail.Should().Be("STAFF@GEARFLOW.COM");
        user.NormalizedUserName.Should().Be("STAFF");
        user.IsActive.Should().BeTrue();
        user.SecurityStamp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_with_invalid_email_fails()
    {
        var result = User.Create("no-arroba", "staff", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidEmail");
    }

    [Fact]
    public void SetPasswordHash_rotates_security_stamp()
    {
        var user = NewUser();
        var before = user.SecurityStamp;

        user.SetPasswordHash("hash", Now);

        user.SecurityStamp.Should().NotBe(before);
        user.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public void Lockout_triggers_after_max_failures()
    {
        var user = NewUser();

        for (var i = 0; i < 5; i++)
            user.RegisterFailedAccess(maxAccessFailedCount: 5, lockoutMinutes: 15, Now);

        user.CanAuthenticateAt(Now).Should().BeFalse();          // bloqueado
        user.CanAuthenticateAt(Now.AddMinutes(16)).Should().BeTrue(); // após o lockout
    }

    [Fact]
    public void ResetAccessFailures_clears_lockout()
    {
        var user = NewUser();
        for (var i = 0; i < 5; i++)
            user.RegisterFailedAccess(5, 15, Now);

        user.ResetAccessFailures(Now);

        user.AccessFailedCount.Should().Be(0);
        user.CanAuthenticateAt(Now).Should().BeTrue();
    }

    [Fact]
    public void Deactivated_user_cannot_authenticate()
    {
        var user = NewUser();
        user.Deactivate(Now);

        user.CanAuthenticateAt(Now).Should().BeFalse();
    }

    [Fact]
    public void IssueRefreshToken_adds_active_token()
    {
        var user = NewUser();

        var token = user.IssueRefreshToken("hash", Now.AddDays(7), "127.0.0.1", Now);

        user.RefreshTokens.Should().ContainSingle();
        token.IsActiveAt(Now).Should().BeTrue();
    }
}
