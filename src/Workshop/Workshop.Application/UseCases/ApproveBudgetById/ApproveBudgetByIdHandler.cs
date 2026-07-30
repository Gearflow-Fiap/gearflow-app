using MediatR;
using Shared.Contracts.IntegrationEvents.Workshop;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.ApproveBudgetById;

/// <summary>
/// Aprova o orçamento (por id) e reserva o estoque, movendo a OS para execução ou aguardando peças.
/// Mesma regra do ApproveBudget keyed por OS, mas resolvendo a OS a partir do orçamento.
/// </summary>
internal sealed class ApproveBudgetByIdHandler : ICommandHandler<ApproveBudgetByIdCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IInventoryReservation _inventory;
    private readonly IPublisher _publisher;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public ApproveBudgetByIdHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, IInventoryReservation inventory,
        IPublisher publisher, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _inventory = inventory;
        _publisher = publisher;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ApproveBudgetByIdCommand command, CancellationToken ct)
    {
        var budget = await _budgets.GetByIdAsync(BudgetId.From(command.BudgetId), ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado."));

        var order = await _orders.GetByIdAsync(budget.ServiceOrderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var actor = _currentActor.Current;

        var approve = budget.Approve(now);
        if (approve.IsFailure) return approve;

        var items = budget.Parts
            .Select(p => new ReservationItem("Part", p.PartId, p.Quantity))
            .Concat(budget.Consumables.Select(c => new ReservationItem("Consumable", c.ConsumableId, c.Quantity)))
            .ToList();

        var reservation = await _inventory.ReserveAsync(items, ct);

        var reserved = reservation.Status == ReservationStatus.Ok;
        var transition = reserved
            ? order.StartExecution(actor, now)
            : order.WaitingPartsOrConsumables(actor, now);
        if (transition.IsFailure) return transition;

        await _orders.SaveChangesAsync(ct);   // OS e Budget no mesmo DbContext → uma transação

        await _publisher.Publish(
            new BudgetApprovedIntegrationEvent(Guid.NewGuid(), now, budget.Id.Value, order.Id.Value, reserved), ct);
        if (!reserved)
            await _publisher.Publish(
                new StockMissingIntegrationEvent(Guid.NewGuid(), now, budget.Id.Value, order.Id.Value,
                    reservation.Detail ?? "Estoque insuficiente."), ct);

        return Result.Success();
    }
}
