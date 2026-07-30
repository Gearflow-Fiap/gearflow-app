using Microsoft.EntityFrameworkCore;
using Workshop.Application.Abstractions;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Infrastructure.Persistence.Repositories;

internal sealed class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly WorkshopDbContext _db;

    public ServiceOrderRepository(WorkshopDbContext db) => _db = db;

    public async Task AddAsync(ServiceOrder order, CancellationToken ct = default) =>
        await _db.ServiceOrders.AddAsync(order, ct);

    public async Task<ServiceOrder?> GetByIdAsync(ServiceOrderId id, CancellationToken ct = default) =>
        await _db.ServiceOrders
            .Include(o => o.RequestedJobs)
            .Include(o => o.RequestedParts)
            .Include(o => o.Histories)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<ServiceOrder>> ListAsync(CancellationToken ct = default) =>
        await _db.ServiceOrders.AsNoTracking()
            .Include(o => o.Histories)
            .Where(o => o.IsActive)
            .OrderByDescending(o => o.CreatedOn)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
