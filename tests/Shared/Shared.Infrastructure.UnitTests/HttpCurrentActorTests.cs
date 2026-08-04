using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Shared.Domain.Security;
using Shared.Infrastructure.Security;

namespace Shared.Infrastructure.UnitTests;

/// <summary>
/// Resolução do <see cref="Actor"/> a partir dos claims do request. Cobre cada ramo:
/// sem contexto, cliente (customer_id), staff (NameIdentifier / sub) e fallback para System.
/// </summary>
public sealed class HttpCurrentActorTests
{
    private sealed class FakeAccessor(HttpContext? ctx) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = ctx;
    }

    private static Actor Resolve(params Claim[] claims)
    {
        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new HttpCurrentActor(new FakeAccessor(ctx)).Current;
    }

    [Fact]
    public void No_http_context_resolves_to_system() =>
        new HttpCurrentActor(new FakeAccessor(null)).Current.Should().Be(Actor.System);

    [Fact]
    public void No_matching_claims_resolves_to_system() =>
        Resolve().Should().Be(Actor.System);

    [Fact]
    public void Customer_id_claim_resolves_to_customer()
    {
        var id = Guid.NewGuid();

        var actor = Resolve(new Claim("customer_id", id.ToString()));

        actor.Should().Be(Actor.Customer(id));
    }

    [Fact]
    public void Invalid_customer_id_falls_through_to_system() =>
        Resolve(new Claim("customer_id", "not-a-guid")).Should().Be(Actor.System);

    [Fact]
    public void NameIdentifier_resolves_to_staff()
    {
        var id = Guid.NewGuid();

        var actor = Resolve(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        actor.Should().Be(Actor.Staff(id));
    }

    [Fact]
    public void Sub_claim_resolves_to_staff_when_nameidentifier_absent()
    {
        var id = Guid.NewGuid();

        var actor = Resolve(new Claim("sub", id.ToString()));

        actor.Should().Be(Actor.Staff(id));
    }

    [Fact]
    public void Invalid_staff_id_resolves_to_system() =>
        Resolve(new Claim(ClaimTypes.NameIdentifier, "nope")).Should().Be(Actor.System);
}
