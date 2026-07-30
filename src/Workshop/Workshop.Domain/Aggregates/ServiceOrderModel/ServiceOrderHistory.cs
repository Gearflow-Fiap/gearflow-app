using Shared.Domain.Primitives;
using Shared.Domain.Security;
using Workshop.Domain.Enums;

namespace Workshop.Domain.Aggregates.ServiceOrderModel;

/// <summary>Trilha de mudança de status da OS, com autoria (<see cref="Actor"/>).</summary>
public sealed class ServiceOrderHistory : Entity<Guid>
{
    public ServiceOrderStatus Status { get; private set; }
    public string Message { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public Actor ChangedBy { get; private set; }

    internal ServiceOrderHistory(ServiceOrderStatus status, string message, Actor changedBy, DateTime createdOn)
        : base(Guid.NewGuid())
    {
        Status = status;
        Message = message;
        ChangedBy = changedBy;
        CreatedOn = createdOn;
    }

    private ServiceOrderHistory() : base(Guid.NewGuid())
    {
        Message = null!;
        ChangedBy = Actor.System;
    }
}
