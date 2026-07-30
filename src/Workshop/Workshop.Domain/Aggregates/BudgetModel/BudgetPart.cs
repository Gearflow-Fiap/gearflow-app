using Shared.Domain.Primitives;

namespace Workshop.Domain.Aggregates.BudgetModel;

/// <summary>Item de peça no orçamento (preço snapshotted + quantidade).</summary>
public sealed class BudgetPart : Entity<Guid>
{
    public Guid PartId { get; private set; }
    public int PriceCents { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }

    public BudgetPart(Guid partId, int priceCents, int quantity, DateTime createdOn) : base(Guid.NewGuid())
    {
        PartId = partId;
        PriceCents = priceCents;
        Quantity = quantity;
        CreatedOn = createdOn;
    }

    private BudgetPart() : base(Guid.NewGuid()) { }
}
