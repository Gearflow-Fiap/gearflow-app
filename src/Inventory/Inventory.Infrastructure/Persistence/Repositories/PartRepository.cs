using Inventory.Application.Abstractions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

internal sealed class PartRepository : IPartRepository
{
    private readonly InventoryDbContext _db;

    public PartRepository(InventoryDbContext db) => _db = db;

    public async Task AddAsync(Part part, CancellationToken ct = default) => await _db.Parts.AddAsync(part, ct);

    public async Task<Part?> GetByIdAsync(PartId id, CancellationToken ct = default) =>
        await _db.Parts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Part>> ListAsync(CancellationToken ct = default) =>
        await _db.Parts.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
