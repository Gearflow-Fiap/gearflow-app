using Inventory.Application.Abstractions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class ConsumableRepository : IConsumableRepository
{
    private readonly InventoryDbContext _db;

    public ConsumableRepository(InventoryDbContext db) => _db = db;

    public async Task AddAsync(Consumable consumable, CancellationToken ct = default) =>
        await _db.Consumables.AddAsync(consumable, ct);

    public async Task<Consumable?> GetByIdAsync(ConsumableId id, CancellationToken ct = default) =>
        await _db.Consumables.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Consumable>> ListAsync(CancellationToken ct = default) =>
        await _db.Consumables.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public void Remove(Consumable consumable) => _db.Consumables.Remove(consumable);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
