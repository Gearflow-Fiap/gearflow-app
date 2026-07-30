using Inventory.Domain.Aggregates;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Abstractions;

public interface IPartRepository
{
    Task AddAsync(Part part, CancellationToken ct = default);
    Task<Part?> GetByIdAsync(PartId id, CancellationToken ct = default);
    Task<IReadOnlyList<Part>> ListAsync(CancellationToken ct = default);
    void Remove(Part part);
    Task SaveChangesAsync(CancellationToken ct = default);
}
