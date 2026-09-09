using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Customers.Infrastructure;
using Customers.Infrastructure.Persistence;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Persistence;
using Shared.Contracts;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Security;
using Testcontainers.MsSql;
using Workshop.Infrastructure;
using Workshop.Infrastructure.Persistence;

namespace Workshop.IntegrationTests.Fixtures;

/// <summary>
/// Sobe um SQL Server real via Testcontainers, cria o banco <c>gearflow</c>, registra a
/// Infrastructure dos 6 BCs contra ele, aplica todas as migrations e expõe um ServiceProvider.
/// Uma instância por coleção de testes (o container é caro de subir).
/// </summary>
public sealed class GearFlowDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Cria um banco dedicado (as migrations usam HasDefaultSchema por BC dentro dele).
        await using (var conn = new SqlConnection(_container.GetConnectionString()))
        {
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("IF DB_ID('gearflow') IS NULL CREATE DATABASE [gearflow];", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "gearflow"
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GearFlow"] = connectionString,
                ["Jwt:Secret"] = "integration-tests-super-secret-key-32chars-min",
                ["Jwt:Issuer"] = "gearflow",
                ["Jwt:Audience"] = "gearflow",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddCurrentActor();                              // ICurrentActor → System (sem HttpContext)
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IBusinessMetrics, NoOpBusinessMetrics>(); // fixture não sobe OpenTelemetry (AddObservability é do Program.cs da API)

        services.AddCatalogInfrastructure(configuration);
        services.AddCustomersInfrastructure(configuration);
        services.AddInventoryInfrastructure(configuration);
        services.AddWorkshopInfrastructure(configuration);
        services.AddIdentityInfrastructure(configuration);
        services.AddNotificationsInfrastructure(configuration);

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<CustomersDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<WorkshopDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public IServiceScope CreateScope() => Services.CreateScope();
}

[CollectionDefinition(nameof(GearFlowDatabaseCollection))]
public sealed class GearFlowDatabaseCollection : ICollectionFixture<GearFlowDatabaseFixture> { }

/// <summary>IBusinessMetrics não-instrumentado só para satisfazer a DI nos testes de integração.</summary>
internal sealed class NoOpBusinessMetrics : IBusinessMetrics
{
    public void ServiceOrderCreated() { }
    public void ServiceOrderStatusDuration(string status, double minutes) { }
    public void IntegrationError(string source) { }
}
