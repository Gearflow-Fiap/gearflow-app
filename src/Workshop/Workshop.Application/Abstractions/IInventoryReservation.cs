namespace Workshop.Application.Abstractions;

/// <summary>Item de estoque a reservar/consumir por conta de um orçamento.</summary>
public sealed record ReservationItem(string ItemType, Guid ItemId, decimal Quantity);

public enum ReservationStatus { Ok, Insufficient }

public sealed record ReservationResult(ReservationStatus Status, string? Detail = null);

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
