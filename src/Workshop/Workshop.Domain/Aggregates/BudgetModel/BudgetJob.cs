using Shared.Domain.Primitives;

namespace Workshop.Domain.Aggregates.BudgetModel;

/// <summary>Item de mão de obra no orçamento, com preço snapshotted e rastreio de execução.</summary>
public sealed class BudgetJob : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public int PriceCents { get; private set; }
    public bool IsExecuted { get; private set; }
    public DateTime? ExecutedOn { get; private set; }
    public DateTime CreatedOn { get; private set; }

    public BudgetJob(Guid jobId, int priceCents, DateTime createdOn) : base(Guid.NewGuid())
    {
        JobId = jobId;
        PriceCents = priceCents;
        CreatedOn = createdOn;
    }

    private BudgetJob() : base(Guid.NewGuid()) { }

    public Result MarkAsExecuted(DateTime nowUtc)
    {
        if (IsExecuted)
            return Result.Failure(Error.Conflict("BudgetJob.AlreadyExecuted", "Este serviço já foi registrado como executado."));

        IsExecuted = true;
        ExecutedOn = nowUtc;
        return Result.Success();
    }
}
