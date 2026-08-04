using FluentAssertions;
using Identity.Domain.Aggregates;

namespace Identity.UnitTests;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    private static RefreshToken AToken() =>
        User.Create("staff@gearflow.com", "staff", Now).Value
            .IssueRefreshToken("hash", Now.AddDays(7), "127.0.0.1", Now);

    [Fact]
    public void IsActiveAt_true_while_not_expired_and_not_revoked()
    {
        AToken().IsActiveAt(Now.AddDays(1)).Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_false_after_expiry()
    {
        AToken().IsActiveAt(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_marks_token_inactive_and_records_metadata()
    {
        var token = AToken();

        token.Revoke("new-hash", "10.0.0.1", Now.AddHours(1));

        token.RevokedAt.Should().Be(Now.AddHours(1));
        token.RevokedByIp.Should().Be("10.0.0.1");
        token.ReplacedByTokenHash.Should().Be("new-hash");
        token.IsActiveAt(Now.AddHours(2)).Should().BeFalse();   // revogado => inativo mesmo antes de expirar
    }
}
