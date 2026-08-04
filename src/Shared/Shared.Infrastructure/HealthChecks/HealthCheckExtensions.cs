using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Infrastructure.HealthChecks;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // wiring de DI/health — fora da meta de cobertura significativa
public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddCustomHealthChecks(
        this IServiceCollection services,
        string databaseConnectionString,
        string apiName)
    {
        return services
            .AddHealthChecks()
            .AddCheck("Liveness",
                () => HealthCheckResult.Healthy("App is running"),
                tags: ["live"])
            .AddAsyncCheck("SqlServer",
                async ct =>
                {
                    try
                    {
                        await using var conn = new SqlConnection(databaseConnectionString);
                        await conn.OpenAsync(ct);
                        await using var cmd = new SqlCommand("SELECT 1", conn);
                        await cmd.ExecuteScalarAsync(ct);
                        return HealthCheckResult.Healthy("SQL Server connection OK");
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy("SQL Server connection failed", ex);
                    }
                },
                tags: ["ready", "database"])
            .AddCheck("Readiness",
                () => HealthCheckResult.Healthy($"{apiName} is ready to serve traffic"),
                tags: ["ready"]);
    }
}
