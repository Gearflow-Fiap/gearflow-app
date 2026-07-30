namespace Shared.Contracts.IntegrationEvents.Inventory;

/// <summary>
/// Publicado pelo Inventory quando um item tem estoque reposto (<c>PATCH stock</c>). Consumido pelo
/// Workshop para reavaliar OS paradas em <c>AwaitingPartsOrConsumables</c> (auto-resume — preserva
/// a regra do GearFlow <c>InventoryService.TryStartAwaitingOrderAsync</c> como evento cross-BC).
/// </summary>
public sealed record PartsReplenishedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    string ItemType,      // "Part" | "Consumable"
    Guid ItemId) : IIntegrationEvent;
