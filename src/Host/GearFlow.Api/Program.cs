using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using GearFlow.Api;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Endpoints;
using Shared.Infrastructure.HealthChecks;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Middleware;
using Shared.Infrastructure.Observability;
using Shared.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Observabilidade + logging estruturado (Fase 3)
builder.AddSerilogLogging("GearFlow.Api");
builder.AddObservability("GearFlow.Api");

// Erro central (ProblemDetails RFC 9457) + auth + ator do request
builder.Services.AddSharedErrorHandling();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCurrentActor();
builder.Services.AddSingleton(TimeProvider.System);

// Health check (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("GearFlow") ?? string.Empty;
builder.Services.AddCustomHealthChecks(connectionString, "GearFlow.Api");

// Bounded Contexts (composition roots) — adicione cada BC migrado aqui
builder.Services.AddCatalogInfrastructure(builder.Configuration);

// OpenAPI / Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

// Migrations no startup (idempotente) — ver ADR de migrations
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

app.MapObservability();       // GET /metrics
app.MapCustomHealthChecks();  // /health, /health/live, /health/ready
app.MapEndpoints(typeof(IApiMarker).Assembly);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

namespace GearFlow.Api
{
    /// <summary>Marcador de assembly para descoberta de <see cref="IEndpoint"/> e testes de integração.</summary>
    public interface IApiMarker { }
}
