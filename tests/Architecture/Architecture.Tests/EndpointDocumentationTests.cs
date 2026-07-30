using FluentAssertions;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architecture.Tests;

/// <summary>
/// Garante que TODO endpoint de negócio (rotas <c>/api/**</c>) declare <c>.WithSummary(...)</c> e
/// <c>.WithDescription(...)</c> — documentação como contrato executável. Sobe o app em TestServer
/// (sem banco: ambiente "Testing" pula as migrations).
/// </summary>
public sealed class EndpointDocumentationTests : IClassFixture<EndpointDocumentationTests.DocFactory>
{
    private readonly DocFactory _factory;

    public EndpointDocumentationTests(DocFactory factory) => _factory = factory;

    [Fact]
    public void Every_api_endpoint_has_summary_and_description()
    {
        using var _ = _factory.CreateClient(); // força a construção do pipeline de rotas

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        endpoints.Should().NotBeEmpty("os endpoints /api/** deveriam ter sido mapeados");

        var missing = endpoints
            .Where(e =>
                e.Metadata.GetMetadata<IEndpointSummaryMetadata>() is null ||
                e.Metadata.GetMetadata<IEndpointDescriptionMetadata>() is null)
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        missing.Should().BeEmpty(
            "todo endpoint deve ter .WithSummary e .WithDescription. Sem documentação: "
            + string.Join(" | ", missing));
    }

    public sealed class DocFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureHostConfiguration(cfg => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GearFlow"] = "Server=doc-test;Database=doc-test;Trusted_Connection=True;",
                ["Jwt:Secret"] = "documentation-tests-secret-key-32chars-min",
            }));
            return base.CreateHost(builder);
        }
    }
}
