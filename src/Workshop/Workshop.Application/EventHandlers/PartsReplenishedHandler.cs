using MediatR;
using Shared.Contracts;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;

namespace Workshop.Application.EventHandlers;

/// <summary>
/// Reage à reposição de estoque (evento in-process do Inventory): reavalia as OS paradas em
/// AwaitingPartsOrConsumables cujo orçamento referencia o item reposto e, se a reserva agora couber,
/// retoma a execução. Preserva o auto-resume (TryStartAwaitingOrderAsync) do GearFlow como assinante cross-BC.
/// </summary>
internal sealed class PartsReplenishedHandler : INotificationHandler<PartsReplenishedIntegrationEvent>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IInventoryReservation _inventory;
    private readonly TimeProvider _timeProvider;

    public PartsReplenishedHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, IInventoryReservation inventory, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _inventory = inventory;
        _timeProvider = timeProvider;
    }

    public async Task Handle(PartsReplenishedIntegrationEvent e, CancellationToken ct)
    {
        var awaiting = await _orders.GetAwaitingPartsAsync(ct); // mais antigas primeiro

        foreach (var order in awaiting)
        {
            var budget = await _budgets.GetByServiceOrderAsync(order.Id, ct);
            if (budget is null)
                continue;

            var references =
                (e.ItemType == InventoryItemType.Part && budget.Parts.Any(p => p.PartId == e.ItemId)) ||
                (e.ItemType == InventoryItemType.Consumable && budget.Consumables.Any(c => c.ConsumableId == e.ItemId));
            if (!references)
                continue;

            var items = budget.Parts
                .Select(p => new ReservationItem(InventoryItemType.Part, p.PartId, p.Quantity))
                .Concat(budget.Consumables.Select(c => new ReservationItem(InventoryItemType.Consumable, c.ConsumableId, c.Quantity)))
                .ToList();

            var reservation = await _inventory.ReserveAsync(items, ct);
            if (reservation.Status != ReservationStatus.Ok)
                continue;

            var resumed = order.ResumeExecution(Actor.System, _timeProvider.GetUtcNow().UtcDateTime);
            if (resumed.IsSuccess)
                await _orders.SaveChangesAsync(ct);
        }
    }
}
