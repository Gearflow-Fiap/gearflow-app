using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.Abstractions;

public interface IServiceOrderRepository
{
    Task AddAsync(ServiceOrder order, CancellationToken ct = default);
    Task<ServiceOrder?> GetByIdAsync(ServiceOrderId id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> ListAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
