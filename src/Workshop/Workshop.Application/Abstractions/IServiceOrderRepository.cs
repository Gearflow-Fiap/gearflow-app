using Workshop.Domain.Aggregates.ServiceOrderModel;
using Workshop.Domain.ValueObjects;

namespace Workshop.Application.Abstractions;

public interface IServiceOrderRepository
{
    Task AddAsync(ServiceOrder order, CancellationToken ct = default);
    Task<ServiceOrder?> GetByIdAsync(ServiceOrderId id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> ListAsync(CancellationToken ct = default);

    /// <summary>Página de OS ativas ordenadas por prioridade de status (preserva GetPagedByPriorityAsync do legado).</summary>
    Task<(IReadOnlyList<ServiceOrder> Items, int TotalCount)> GetPagedByPriorityAsync(
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>OS paradas aguardando peças/insumos, mais antigas primeiro (para o auto-resume). Rastreadas.</summary>
    Task<IReadOnlyList<ServiceOrder>> GetAwaitingPartsAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
