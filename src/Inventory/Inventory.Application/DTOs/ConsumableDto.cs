using Inventory.Domain.Aggregates;

namespace Inventory.Application.DTOs;

public sealed record ConsumableDto(
    Guid Id, string Name, int UnitPriceCents, decimal Quantity, decimal ReservedQuantity, bool BelowMinimum)
{
    public static ConsumableDto FromAggregate(Consumable c) =>
        new(c.Id.Value, c.Name, c.UnitPriceCents, c.Quantity, c.ReservedQuantity, c.IsStockBelowMinimum());
}
