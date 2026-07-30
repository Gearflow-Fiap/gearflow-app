namespace Shared.Contracts.IntegrationEvents.Workshop;

/// <summary>
/// Publicado quando a aprovação do orçamento não consegue reservar o estoque. Consumido por
/// Notifications (aviso ao estoquista). Preserva o StockMissingEvent.
/// </summary>
public sealed record StockMissingIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid BudgetId,
    Guid ServiceOrderId,
    string Detail) : IIntegrationEvent;
