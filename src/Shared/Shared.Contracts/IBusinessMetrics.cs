namespace Shared.Contracts;

/// <summary>
/// Métricas de negócio da Fase 3 (dashboard New Relic) — porta usada pelos handlers de Application,
/// implementada em Shared.Infrastructure sobre o Meter "GearFlow.Business" (OpenTelemetry).
/// </summary>
public interface IBusinessMetrics
{
    /// <summary>Incrementa o contador de OS criadas (<c>serviceorders.created</c>).</summary>
    void ServiceOrderCreated();

    /// <summary>Registra o tempo (min) que a OS passou em uma fase (<c>serviceorder.status.duration{status}</c>).</summary>
    void ServiceOrderStatusDuration(string status, double minutes);

    /// <summary>Incrementa o contador de falhas em integrações cross-BC (<c>integration.errors{source}</c>).</summary>
    void IntegrationError(string source);
}
