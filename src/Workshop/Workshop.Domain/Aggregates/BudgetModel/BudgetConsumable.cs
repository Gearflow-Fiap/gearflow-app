using Shared.Domain.Primitives;

namespace Workshop.Domain.Aggregates.BudgetModel;

/// <summary>Item de insumo no orçamento (preço snapshotted + quantidade fracionária).</summary>
public sealed class BudgetConsumable : Entity<Guid>
{
    public Guid ConsumableId { get; private set; }
    public int PriceCents { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }

    public BudgetConsumable(Guid consumableId, int priceCents, decimal quantity, DateTime createdOn) : base(Guid.NewGuid())
    {
        ConsumableId = consumableId;
        PriceCents = priceCents;
        Quantity = quantity;
        CreatedOn = createdOn;
    }

    private BudgetConsumable() : base(Guid.NewGuid()) { }
}
