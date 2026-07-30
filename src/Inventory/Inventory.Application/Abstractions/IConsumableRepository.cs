using Inventory.Domain.Aggregates;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Abstractions;

public interface IConsumableRepository
{
    Task AddAsync(Consumable consumable, CancellationToken ct = default);
    Task<Consumable?> GetByIdAsync(ConsumableId id, CancellationToken ct = default);
    Task<IReadOnlyList<Consumable>> ListAsync(CancellationToken ct = default);
    void Remove(Consumable consumable);
    Task SaveChangesAsync(CancellationToken ct = default);
}
