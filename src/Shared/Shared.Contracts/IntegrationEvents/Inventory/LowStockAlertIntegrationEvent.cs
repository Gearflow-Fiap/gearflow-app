namespace Shared.Contracts.IntegrationEvents.Inventory;

/// <summary>
/// Publicado pelo Inventory quando um item (peça/insumo) cai no/abaixo do mínimo após um consumo.
/// Consumido por Notifications (alerta ao estoquista). Preserva a regra do GearFlow
/// (<c>LowStockAlertEvent</c>), agora como contrato cross-BC.
/// </summary>
public sealed record LowStockAlertIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    InventoryItemType ItemType,
    Guid ItemId,
    string ItemName,
    decimal RemainingQuantity,
    decimal MinimumQuantity) : IIntegrationEvent;
