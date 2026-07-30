using Shared.Domain.Primitives;

namespace Workshop.Domain.Aggregates.ServiceOrderModel;

/// <summary>Serviço (Job) solicitado numa OS.</summary>
public sealed class ServiceOrderJob : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public DateTime CreatedOn { get; private set; }

    public ServiceOrderJob(Guid jobId, DateTime createdOn) : base(Guid.NewGuid())
    {
        JobId = jobId;
        CreatedOn = createdOn;
    }

    private ServiceOrderJob() : base(Guid.NewGuid()) { }
}
