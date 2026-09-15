using System.Diagnostics.Metrics;
using Shared.Contracts;

namespace Shared.Infrastructure.Observability;

/// <summary>
/// Implementação de <see cref="IBusinessMetrics"/> sobre o Meter compartilhado
/// (<see cref="ObservabilityExtensions.BusinessMeterName"/>). Singleton — os instrumentos são
/// criados uma única vez por processo.
/// </summary>
internal sealed class BusinessMetrics : IBusinessMetrics
{
    private readonly Counter<long> _serviceOrdersCreated;
    private readonly Histogram<double> _serviceOrderStatusDuration;
    private readonly Counter<long> _integrationErrors;

    public BusinessMetrics()
    {
        var meter = new Meter(ObservabilityExtensions.BusinessMeterName);

        _serviceOrdersCreated = meter.CreateCounter<long>(
            "serviceorders.created", description: "Volume de ordens de serviço criadas.");

        _serviceOrderStatusDuration = meter.CreateHistogram<double>(
            "serviceorder.status.duration", unit: "min", description: "Tempo (min) por fase da OS.");

        _integrationErrors = meter.CreateCounter<long>(
            "integration.errors", description: "Falhas em integrações/portas cross-BC.");
    }

    public void ServiceOrderCreated() => _serviceOrdersCreated.Add(1);

    public void ServiceOrderStatusDuration(string status, double minutes) =>
        _serviceOrderStatusDuration.Record(minutes, new KeyValuePair<string, object?>("status", status));

    public void IntegrationError(string source) =>
        _integrationErrors.Add(1, new KeyValuePair<string, object?>("source", source));
}
