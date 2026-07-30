using Shared.Domain.Primitives;
using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.UseCases.GetBudgetById;

internal sealed class GetBudgetByIdHandler : IQueryHandler<GetBudgetByIdQuery, BudgetDto>
{
    private readonly IBudgetRepository _repository;

    public GetBudgetByIdHandler(IBudgetRepository repository) => _repository = repository;

    public async Task<Result<BudgetDto>> Handle(GetBudgetByIdQuery query, CancellationToken ct)
    {
        var budget = await _repository.GetByIdAsync(BudgetId.From(query.BudgetId), ct);
        if (budget is null)
            return Result.Failure<BudgetDto>(Error.NotFound("Budget.NotFound", "Orçamento não encontrado."));

        return Result.Success(BudgetDto.FromAggregate(budget));
    }
}
