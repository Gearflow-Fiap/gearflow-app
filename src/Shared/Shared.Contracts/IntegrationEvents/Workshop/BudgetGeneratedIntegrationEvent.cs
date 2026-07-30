namespace Shared.Contracts.IntegrationEvents.Workshop;

/// <summary>
/// Publicado quando o orçamento é gerado (finalização do diagnóstico). Consumido por Notifications
/// para enviar o e-mail ao cliente com os links de aprovar/rejeitar. Preserva o BudgetGeneratedEvent.
/// </summary>
public sealed record BudgetGeneratedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid BudgetId,
    Guid ServiceOrderId,
    string ClientName,
    string ClientEmail,
    int TotalPriceCents) : IIntegrationEvent;
