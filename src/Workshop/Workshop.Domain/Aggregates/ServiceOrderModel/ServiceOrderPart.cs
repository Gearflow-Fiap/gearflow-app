using Shared.Domain.Primitives;

namespace Workshop.Domain.Aggregates.ServiceOrderModel;

/// <summary>Peça solicitada numa OS, com quantidade.</summary>
public sealed class ServiceOrderPart : Entity<Guid>
{
    public Guid PartId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }

    public ServiceOrderPart(Guid partId, int quantity, DateTime createdOn) : base(Guid.NewGuid())
    {
        PartId = partId;
        Quantity = quantity;
        CreatedOn = createdOn;
    }

    private ServiceOrderPart() : base(Guid.NewGuid()) { }
}
