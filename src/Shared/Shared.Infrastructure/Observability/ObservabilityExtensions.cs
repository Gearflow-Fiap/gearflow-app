using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Contracts;

namespace Shared.Infrastructure.Observability;

/// <summary>
/// Wires OpenTelemetry tracing + metrics for the API (Fase 3 — observabilidade).
///
/// Tracing sources: ASP.NET Core, HttpClient. Traces export via OTLP quando <c>Otlp:Endpoint</c>
/// está configurado (ex.: coletor Datadog/New Relic/Jaeger); caso contrário o tracing fica
/// in-process e nenhum exporter é acionado.
///
/// Metrics: ASP.NET Core, HttpClient, runtime/GC — sempre expostos em <c>/metrics</c> para
/// scraping Prometheus (via <see cref="MapObservability"/>).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // wiring de OpenTelemetry — fora da meta de cobertura
public static class ObservabilityExtensions
{
    /// <summary>
    /// Meter compartilhado para métricas de negócio. Cada BC define seu próprio <c>Meter</c> com
    /// este mesmo nome (ex.: volume diário de OS, tempo por status). Uso: <c>new Meter(BusinessMeterName)</c>.
    /// </summary>
    public const string BusinessMeterName = "GearFlow.Business";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder, string serviceName)
    {
        var configuration = builder.Configuration;
        var environmentName = builder.Environment.EnvironmentName;
        var otlpEndpoint = configuration["Otlp:Endpoint"];
        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environmentName
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(BusinessMeterName)
                    .AddPrometheusExporter();

                // Métricas de negócio (serviceorders.created, serviceorder.status.duration,
                // integration.errors) também saem via OTLP quando configurado — é o que alimenta
                // o dashboard custom no New Relic (ver newrelic-dashboard.tf).
                // Delta (não Cumulative): cada export manda só o incremento desde o export anterior,
                // evitando somas/médias duplicadas quando o New Relic agrega várias leituras na janela.
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter((exporterOptions, readerOptions) =>
                    {
                        exporterOptions.Endpoint = new Uri(otlpEndpoint);
                        readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                    });
            });

        builder.Services.AddSingleton<IBusinessMetrics, BusinessMetrics>();

        return builder;
    }

    /// <summary>Maps the Prometheus scraping endpoint at <c>/metrics</c>. Call after <c>Build()</c>.</summary>
    public static WebApplication MapObservability(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(); // GET /metrics
        return app;
    }
}
