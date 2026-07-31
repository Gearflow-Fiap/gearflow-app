namespace Workshop.Application.Abstractions;

/// <summary>Preços autoritativos lidos ao vivo dos BCs Catalog/Inventory (server-authoritative).</summary>
public sealed record JobPrice(Guid JobId, int PriceCents);
public sealed record PartPrice(Guid PartId, int PriceCents);
public sealed record ConsumablePrice(Guid ConsumableId, int UnitPriceCents);

/// <summary>
/// Lê os preços atuais de serviços/peças/insumos para o orçamento fazer o snapshot. Implementada na
/// Infrastructure via SQL cru contra catalog.jobs / inventory.parts / inventory.consumables (ADR-002).
/// </summary>
public interface IPricingReader
{
    Task<IReadOnlyDictionary<Guid, int>> GetJobPricesAsync(IEnumerable<Guid> jobIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> GetPartPricesAsync(IEnumerable<Guid> partIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> GetConsumablePricesAsync(IEnumerable<Guid> consumableIds, CancellationToken ct = default);
}
