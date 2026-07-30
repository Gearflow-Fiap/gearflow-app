using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.RejectBudget;

/// <summary>Rejeita o orçamento e cancela a OS (preserva o RejectBudget do GearFlow).</summary>
internal sealed class RejectBudgetHandler : ICommandHandler<RejectBudgetCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public RejectBudgetHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RejectBudgetCommand command, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(command.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado para esta OS."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var reject = budget.Reject();
        if (reject.IsFailure) return reject;

        var cancel = order.Cancel(_currentActor.Current, now);
        if (cancel.IsFailure) return cancel;

        await _orders.SaveChangesAsync(ct);
        return Result.Success();
    }
}
