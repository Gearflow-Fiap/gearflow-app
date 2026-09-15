using MediatR;
using Shared.Contracts;
using Shared.Contracts.IntegrationEvents.Workshop;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.ApproveBudget;

/// <summary>
/// Aprova o orçamento e reserva o estoque (via <see cref="IInventoryReservation"/>). Preserva o
/// branch do GearFlow: reserva OK → OS em execução; estoque insuficiente → OS aguardando peças.
/// </summary>
internal sealed class ApproveBudgetHandler : ICommandHandler<ApproveBudgetCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IInventoryReservation _inventory;
    private readonly IPublisher _publisher;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;
    private readonly IBusinessMetrics _metrics;

    public ApproveBudgetHandler(
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

    public async Task<Result> Handle(ApproveBudgetCommand command, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(command.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado para esta OS."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var actor = _currentActor.Current;

        var approve = budget.Approve(now);
        if (approve.IsFailure) return approve;

        var items = budget.Parts
            .Select(p => new ReservationItem(InventoryItemType.Part, p.PartId, p.Quantity))
            .Concat(budget.Consumables.Select(c => new ReservationItem(InventoryItemType.Consumable, c.ConsumableId, c.Quantity)))
            .ToList();

        var reservation = await _inventory.ReserveAsync(items, ct);

        var reserved = reservation.Status == ReservationStatus.Ok;
        if (!reserved) _metrics.IntegrationError("Inventory.Reserve");

        var transition = reserved
            ? order.StartExecution(actor, now)
            : order.WaitingPartsOrConsumables(actor, now);
        if (transition.IsFailure) return transition;

        await _orders.SaveChangesAsync(ct);

        await _publisher.Publish(
            new BudgetApprovedIntegrationEvent(Guid.NewGuid(), now, budget.Id.Value, order.Id.Value, reserved), ct);
        if (!reserved)
            await _publisher.Publish(
                new StockMissingIntegrationEvent(Guid.NewGuid(), now, budget.Id.Value, order.Id.Value,
                    reservation.Detail ?? "Estoque insuficiente."), ct);

        return Result.Success();
    }
}
