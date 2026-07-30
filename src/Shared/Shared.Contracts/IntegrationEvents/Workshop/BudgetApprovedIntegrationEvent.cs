namespace Shared.Contracts.IntegrationEvents.Workshop;

/// <summary>
/// Publicado quando um orçamento é aprovado. Consumido por Notifications (aviso interno à oficina).
/// <see cref="StockReserved"/> distingue "em execução" de "aguardando reposição". Preserva o BudgetApprovedEvent.
/// </summary>
public sealed record BudgetApprovedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid BudgetId,
    Guid ServiceOrderId,
    bool StockReserved) : IIntegrationEvent;
