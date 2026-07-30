using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Domain.Enums;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.ExecuteJob;

/// <summary>
/// Marca um serviço do orçamento como executado (progresso granular). A OS precisa estar em execução,
/// como no ExecuteJobUseCase do legado. Idempotência garantida no agregado (BudgetJob.MarkAsExecuted).
/// </summary>
internal sealed class ExecuteJobHandler : ICommandHandler<ExecuteJobCommand>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;
    private readonly TimeProvider _timeProvider;

    public ExecuteJobHandler(IServiceOrderRepository orders, IBudgetRepository budgets, TimeProvider timeProvider)
    {
        _orders = orders;
        _budgets = budgets;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(ExecuteJobCommand command, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(command.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        if (order.Status != ServiceOrderStatus.InExecution)
            return Result.Failure(Error.Conflict("ServiceOrder.NotInExecution", "A OS deve estar 'InExecution' para executar serviços."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);
        if (budget is null)
            return Result.Failure(Error.NotFound("Budget.NotFound", "Orçamento não encontrado para esta OS."));

        var result = budget.MarkJobAsExecuted(command.BudgetJobId, _timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure) return result;

        await _budgets.SaveChangesAsync(ct);
        return Result.Success();
    }
}
