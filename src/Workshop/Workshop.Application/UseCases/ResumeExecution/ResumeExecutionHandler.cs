using Shared.Contracts;
using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.ResumeExecution;

/// <summary>
/// Retoma manualmente uma OS parada em AwaitingPartsOrConsumables: re-tenta a reserva de estoque e
/// só avança para InExecution se houver disponibilidade (preserva o ResumeExecutionFromWaitingParts do legado).
/// </summary>
internal sealed class ResumeExecutionHandler : ICommandHandler<ResumeExecutionCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly IInventoryReservation _inventory;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;
    private readonly IBusinessMetrics _metrics;

    public ResumeExecutionHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, IInventoryReservation inventory,
        ICurrentActor currentActor, TimeProvider timeProvider, IBusinessMetrics metrics)
    {
        _orders = orders;
        _budgets = budgets;
        _inventory = inventory;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
        _metrics = metrics;
    }

    public async Task<Result> Handle(ResumeExecutionCommand command, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(command.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado para esta OS."));

        var items = budget.Parts
            .Select(p => new ReservationItem(InventoryItemType.Part, p.PartId, p.Quantity))
            .Concat(budget.Consumables.Select(c => new ReservationItem(InventoryItemType.Consumable, c.ConsumableId, c.Quantity)))
            .ToList();

        var reservation = await _inventory.ReserveAsync(items, ct);
        if (reservation.Status != ReservationStatus.Ok)
        {
            _metrics.IntegrationError("Inventory.Reserve");
            return Result.Failure(Error.Conflict("Inventory.StillInsufficient",
                reservation.Detail ?? "Estoque ainda insuficiente para retomar a execução."));
        }

        var transition = order.ResumeExecution(_currentActor.Current, _timeProvider.GetUtcNow().UtcDateTime);
        if (transition.IsFailure) return transition;

        await _orders.SaveChangesAsync(ct);
        return Result.Success();
    }
}
