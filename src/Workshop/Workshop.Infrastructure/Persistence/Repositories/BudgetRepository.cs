using Microsoft.EntityFrameworkCore;
using Workshop.Application.Abstractions;
using Workshop.Domain.Aggregates.BudgetModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Infrastructure.Persistence.Repositories;

internal sealed class BudgetRepository : IBudgetRepository
{
    private readonly WorkshopDbContext _db;

    public BudgetRepository(WorkshopDbContext db) => _db = db;

    public async Task AddAsync(Budget budget, CancellationToken ct = default) =>
        await _db.Budgets.AddAsync(budget, ct);

    public async Task<Budget?> GetByIdAsync(BudgetId id, CancellationToken ct = default) =>
        await IncludeItems(_db.Budgets).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Budget?> GetByServiceOrderAsync(ServiceOrderId serviceOrderId, CancellationToken ct = default) =>
        await IncludeItems(_db.Budgets).FirstOrDefaultAsync(b => b.ServiceOrderId == serviceOrderId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    private static IQueryable<Budget> IncludeItems(IQueryable<Budget> q) =>
        q.Include(b => b.Jobs).Include(b => b.Parts).Include(b => b.Consumables);
}
