using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Architecture.Tests;

/// <summary>
/// Contrato de autenticação na borda: rotas de staff exigem JWT (401 sem token). Sobe o app em
/// TestServer no ambiente "Testing" (sem banco) — o 401 vem do middleware de auth antes de tocar
/// qualquer DbContext, então o teste roda no CI sem Docker.
/// </summary>
public sealed class AuthorizationContractTests : IClassFixture<AuthorizationContractTests.ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationContractTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/catalog/jobs")]
    [InlineData("/api/inventory/parts")]
    [InlineData("/api/workshop/service-orders")]
    public async Task Protected_route_without_token_returns_401(string route)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); // pula migrations — sem banco
            builder.ConfigureHostConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GearFlow"] = "Server=auth-test;Database=auth-test;Trusted_Connection=True;",
                ["Jwt:Secret"] = "authorization-tests-secret-key-32chars-min",
            }));
            return base.CreateHost(builder);
        }
    }
}
