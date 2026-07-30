using Microsoft.EntityFrameworkCore;
using Workshop.Application.Abstractions;
using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.Enums;
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

    public async Task<(IReadOnlyList<ServiceOrder> Items, int TotalCount)> GetPagedByPriorityAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        // Prioridade de status idêntica ao legado: em execução primeiro, entregues/finalizadas fora.
        var baseQuery = _db.ServiceOrders.AsNoTracking()
            .Include(o => o.Histories)
            .Where(o => o.IsActive
                        && o.Status != ServiceOrderStatus.Finalized
                        && o.Status != ServiceOrderStatus.Delivered)
            .OrderBy(o => o.Status == ServiceOrderStatus.InExecution ? 1
                        : o.Status == ServiceOrderStatus.AwaitingApproval ? 2
                        : o.Status == ServiceOrderStatus.AwaitingPartsOrConsumables ? 3
                        : o.Status == ServiceOrderStatus.InDiagnostic ? 4
                        : o.Status == ServiceOrderStatus.Received ? 5
                        : 6)
            .ThenBy(o => o.CreatedOn);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<ServiceOrder>> GetAwaitingPartsAsync(CancellationToken ct = default) =>
        await _db.ServiceOrders   // rastreadas: serão mutadas no auto-resume
            .Include(o => o.Histories)
            .Where(o => o.IsActive && o.Status == ServiceOrderStatus.AwaitingPartsOrConsumables)
            .OrderBy(o => o.CreatedOn)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
