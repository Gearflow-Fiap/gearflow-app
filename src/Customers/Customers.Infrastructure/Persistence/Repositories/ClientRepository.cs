using Customers.Application.Abstractions;
using Customers.Domain.Aggregates;
using Customers.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Persistence.Repositories;

internal sealed class ClientRepository : IClientRepository
{
    private readonly CustomersDbContext _db;

    public ClientRepository(CustomersDbContext db) => _db = db;

    public async Task AddAsync(Client client, CancellationToken ct = default) =>
        await _db.Clients.AddAsync(client, ct);

    public async Task<Client?> GetByIdAsync(ClientId id, CancellationToken ct = default) =>
        await _db.Clients.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default) =>
        await _db.Clients.AsNoTracking().Include(c => c.Vehicles).OrderBy(c => c.Name).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
