using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.GetServiceOrderDetails;

/// <summary>OS + orçamento numa única resposta (equivalente ao GET /{id}/details do legado).</summary>
internal sealed class GetServiceOrderDetailsHandler : IQueryHandler<GetServiceOrderDetailsQuery, ServiceOrderDetailsDto>
{
    private readonly IServiceOrderRepository _orders;
    private readonly IBudgetRepository _budgets;

    public GetServiceOrderDetailsHandler(IServiceOrderRepository orders, IBudgetRepository budgets)
    {
        _orders = orders;
        _budgets = budgets;
    }

    public async Task<Result<ServiceOrderDetailsDto>> Handle(GetServiceOrderDetailsQuery query, CancellationToken ct)
    {
        var orderId = ServiceOrderId.From(query.ServiceOrderId);
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result.Failure<ServiceOrderDetailsDto>(Error.NotFound("ServiceOrder.NotFound", "Ordem de serviço não encontrada."));

        var budget = await _budgets.GetByServiceOrderAsync(orderId, ct);

        return Result.Success(new ServiceOrderDetailsDto(
            ServiceOrderDto.FromAggregate(order),
            budget is null ? null : BudgetDto.FromAggregate(budget)));
    }
}
