using MediatR;
using Shared.Contracts;
using Shared.Contracts.IntegrationEvents.Inventory;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.Enums;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.FinalizeServiceOrder;

/// <summary>
/// Finaliza a OS (InExecution → Finalized) e consome o estoque reservado (via
/// <see cref="IInventoryReservation"/>). Preserva o consumo-na-finalização do GearFlow.
/// </summary>
internal sealed class FinalizeServiceOrderHandler : ICommandHandler<FinalizeServiceOrderCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IInventoryReservation _inventory;
    private readonly IPublisher _publisher;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;
    private readonly IBusinessMetrics _metrics;

    public FinalizeServiceOrderHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, IInventoryReservation inventory,
        IPublisher publisher, ICurrentActor currentActor, TimeProvider timeProvider, IBusinessMetrics metrics)
    {
        _orders = orders;
        _budgets = budgets;
        _inventory = inventory;
        _publisher = publisher;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
        _metrics = metrics;
    }

    public async Task<Result> Handle(FinalizeServiceOrderCommand command, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(command.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado para esta OS."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var executionStart = order.Histories
            .Where(h => h.Status == ServiceOrderStatus.InExecution)
            .OrderBy(h => h.CreatedOn).FirstOrDefault();

        var transition = order.Finalize(_currentActor.Current, now);
        if (transition.IsFailure) return transition;

        if (executionStart is not null)
            _metrics.ServiceOrderStatusDuration("Execucao", (now - executionStart.CreatedOn).TotalMinutes);

        var items = budget.Parts
            .Select(p => new ReservationItem(InventoryItemType.Part, p.PartId, p.Quantity))
            .Concat(budget.Consumables.Select(c => new ReservationItem(InventoryItemType.Consumable, c.ConsumableId, c.Quantity)))
            .ToList();

        var consume = await _inventory.ConsumeAsync(items, ct);
        if (consume.Status != ReservationStatus.Ok)
        {
            _metrics.IntegrationError("Inventory.Consume");
            return Result.Failure(Error.Conflict("Inventory.ConsumeFailed", consume.Detail ?? "Falha ao consumir estoque."));
        }

        await _orders.SaveChangesAsync(ct);

        // Alerta de estoque baixo → Notifications (evento in-process). Preserva o LowStockAlert do GearFlow.
        foreach (var low in consume.LowStock ?? [])
            await _publisher.Publish(
                new LowStockAlertIntegrationEvent(
                    Guid.NewGuid(), now, low.ItemType, low.ItemId, low.ItemName, low.Remaining, low.Minimum),
                ct);

        return Result.Success();
    }
}
