using Catalog.Domain.Aggregates;

namespace Catalog.Application.DTOs;

/// <summary>Projeção pública de <see cref="Job"/>. Factory estática no próprio record (sem AutoMapper).</summary>
public sealed record JobDto(Guid Id, string Name, string Description, int PriceCents)
{
    public static JobDto FromAggregate(Job job) =>
        new(job.Id.Value, job.Name, job.Description, job.PriceCents);
}
