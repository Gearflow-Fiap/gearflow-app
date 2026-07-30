using Catalog.Application.Abstractions;
using Catalog.Domain.Aggregates;
using Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence.Repositories;

internal sealed class JobRepository : IJobRepository
{
    private readonly CatalogDbContext _db;

    public JobRepository(CatalogDbContext db) => _db = db;

    public async Task AddAsync(Job job, CancellationToken ct = default) =>
        await _db.Jobs.AddAsync(job, ct);

    public async Task<Job?> GetByIdAsync(JobId id, CancellationToken ct = default) =>
        await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<Job>> ListAsync(CancellationToken ct = default) =>
        await _db.Jobs.AsNoTracking().OrderBy(j => j.Name).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
