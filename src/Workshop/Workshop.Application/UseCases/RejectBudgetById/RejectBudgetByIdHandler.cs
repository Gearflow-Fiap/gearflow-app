using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Application.Abstractions;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.RejectBudgetById;

/// <summary>Rejeita o orçamento (por id) e cancela a OS.</summary>
internal sealed class RejectBudgetByIdHandler : ICommandHandler<RejectBudgetByIdCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly ICurrentActor _currentActor;
    private readonly TimeProvider _timeProvider;

    public RejectBudgetByIdHandler(
        IServiceOrderRepository orders, IBudgetRepository budgets, ICurrentActor currentActor, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _currentActor = currentActor;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RejectBudgetByIdCommand command, CancellationToken ct)
    {
        var budget = await _budgets.GetByIdAsync(BudgetId.From(command.BudgetId), ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado."));

        var order = await _orders.GetByIdAsync(budget.ServiceOrderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var reject = budget.Reject();
        if (reject.IsFailure) return reject;

        var cancel = order.Cancel(_currentActor.Current, now);
        if (cancel.IsFailure) return cancel;

        await _orders.SaveChangesAsync(ct);
        return Result.Success();
    }
}
