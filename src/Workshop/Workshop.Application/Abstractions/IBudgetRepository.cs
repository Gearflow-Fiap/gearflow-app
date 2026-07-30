using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.Abstractions;

public interface IBudgetRepository
{
    Task AddAsync(Budget budget, CancellationToken ct = default);
    Task<Budget?> GetByIdAsync(BudgetId id, CancellationToken ct = default);
    Task<Budget?> GetByServiceOrderAsync(ServiceOrderId serviceOrderId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
