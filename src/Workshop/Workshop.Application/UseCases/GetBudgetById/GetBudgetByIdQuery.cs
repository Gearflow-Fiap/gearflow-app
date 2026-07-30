using Workshop.Application.Abstractions;
using Workshop.Application.DTOs;

namespace Workshop.Application.UseCases.GetBudgetById;

public sealed record GetBudgetByIdQuery(Guid BudgetId) : IQuery<BudgetDto>;
