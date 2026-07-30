using Catalog.Domain.Aggregates;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Abstractions;

public interface IJobRepository
{
    Task AddAsync(Job job, CancellationToken ct = default);
    Task<Job?> GetByIdAsync(JobId id, CancellationToken ct = default);
    Task<IReadOnlyList<Job>> ListAsync(CancellationToken ct = default);
    void Remove(Job job);
    Task SaveChangesAsync(CancellationToken ct = default);
}
