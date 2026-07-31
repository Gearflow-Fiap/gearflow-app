using FluentAssertions;
using Identity.Application.UseCases.LoginUser;
using Identity.Application.UseCases.RefreshAccessToken;
using Identity.Application.UseCases.RegisterUser;
using Identity.Application.UseCases.RevokeToken;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shared.Domain.Primitives;
using Workshop.IntegrationTests.Fixtures;

namespace Workshop.IntegrationTests;

/// <summary>
/// Persistência do Identity contra o SQL Server real: registro, login, rotação de refresh token e
/// revogação. Cobre exatamente a classe de bug que já mordeu (novo filho <c>RefreshToken</c> tratado
/// como UPDATE → "0 rows"), que testes com mock não pegam.
/// </summary>
[Collection(nameof(GearFlowDatabaseCollection))]
public sealed class IdentityPersistenceTests
{
    private readonly GearFlowDatabaseFixture _fixture;

    public IdentityPersistenceTests(GearFlowDatabaseFixture fixture) => _fixture = fixture;

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _fixture.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    [Fact]
    public async Task Register_login_refresh_and_revoke_round_trip()
    {
        var email = $"user-{Guid.NewGuid():N}@gearflow.com";
        (await Send(new RegisterUserCommand(email, "staff", "Secret123"))).IsSuccess.Should().BeTrue();

        var login = await Send(new LoginUserCommand(email, "Secret123", "127.0.0.1"));
        login.IsSuccess.Should().BeTrue();
        login.Value.RefreshToken.Should().NotBeNullOrEmpty();

        // Refresh persiste um novo token e rotaciona o antigo (INSERT do filho num agregado já
        // rastreado) — o bug de child-key apareceria aqui como DbUpdateException.
        var refreshed = await Send(new RefreshAccessTokenCommand(login.Value.RefreshToken, "127.0.0.1"));
        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.Value.RefreshToken.Should().NotBe(login.Value.RefreshToken);

        // O token antigo foi revogado na rotação: não vale mais.
        (await Send(new RefreshAccessTokenCommand(login.Value.RefreshToken, "127.0.0.1")))
            .Error.Type.Should().Be(ErrorType.Unauthorized);

        // Revoga o token novo → também deixa de valer.
        (await Send(new RevokeTokenCommand(refreshed.Value.RefreshToken, "127.0.0.1"))).IsSuccess.Should().BeTrue();
        (await Send(new RefreshAccessTokenCommand(refreshed.Value.RefreshToken, "127.0.0.1")))
            .Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Duplicate_registration_is_rejected()
    {
        var email = $"dup-{Guid.NewGuid():N}@gearflow.com";
        (await Send(new RegisterUserCommand(email, "staff", "Secret123"))).IsSuccess.Should().BeTrue();

        var again = await Send(new RegisterUserCommand(email, "staff", "Secret123"));

        again.IsFailure.Should().BeTrue();
        again.Error.Code.Should().Be("User.EmailAlreadyExists");
    }
}
