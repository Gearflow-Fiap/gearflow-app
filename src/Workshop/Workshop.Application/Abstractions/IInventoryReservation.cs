using Shared.Contracts;

namespace Workshop.Application.Abstractions;

/// <summary>Item de estoque a reservar/consumir por conta de um orçamento.</summary>
public sealed record ReservationItem(InventoryItemType ItemType, Guid ItemId, decimal Quantity);

public enum ReservationStatus { Ok, Insufficient }

/// <summary>Item que ficou no/abaixo do estoque mínimo após um consumo — dispara o alerta.</summary>
public sealed record LowStockItem(InventoryItemType ItemType, Guid ItemId, string ItemName, decimal Remaining, decimal Minimum);

public sealed record ReservationResult(
    ReservationStatus Status,
    string? Detail = null,
    IReadOnlyList<LowStockItem>? LowStock = null);

/// <summary>
/// Porta do Workshop para o estoque (Inventory) — preserva a regra do GearFlow de reservar na
/// aprovação do orçamento e consumir na finalização, sem que o Workshop referencie o Inventory.
/// Implementada na Infrastructure via SQL cru contra as tabelas do Inventory (ADR-011).
/// </summary>
public interface IInventoryReservation
{
    /// <summary>Reserva TODOS os itens atômicamente. Se algum não tiver disponibilidade, nada é reservado.</summary>
    Task<ReservationResult> ReserveAsync(IReadOnlyList<ReservationItem> items, CancellationToken ct = default);

    /// <summary>Consome (baixa a reserva) de todos os itens.</summary>
    Task<ReservationResult> ConsumeAsync(IReadOnlyList<ReservationItem> items, CancellationToken ct = default);
}
