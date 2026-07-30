using Shared.Domain.Primitives;
using Workshop.Domain.ValueObjects;

namespace Workshop.Domain.Aggregates.BudgetModel;

/// <summary>
/// Orçamento de uma OS. Nasce na finalização do diagnóstico com os itens (job/peça/insumo) e o
/// <see cref="TotalPriceCents"/> = soma dos preços snapshotted. <see cref="IsApproved"/> nulo = ainda
/// não revisado. Aprovação/rejeição são idempotentes (uma vez só), via <see cref="Result"/>.
/// </summary>
public sealed class Budget : AggregateRoot<BudgetId>
{
    private readonly List<BudgetJob> _jobs = new();
    private readonly List<BudgetPart> _parts = new();
    private readonly List<BudgetConsumable> _consumables = new();

    public ServiceOrderId ServiceOrderId { get; private set; }
    public int TotalPriceCents { get; private set; }
    public bool? IsApproved { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? ApprovedOn { get; private set; }

    public IReadOnlyCollection<BudgetJob> Jobs => _jobs.AsReadOnly();
    public IReadOnlyCollection<BudgetPart> Parts => _parts.AsReadOnly();
    public IReadOnlyCollection<BudgetConsumable> Consumables => _consumables.AsReadOnly();

    private Budget(
        BudgetId id, ServiceOrderId serviceOrderId,
        List<BudgetJob> jobs, List<BudgetPart> parts, List<BudgetConsumable> consumables, DateTime nowUtc)
        : base(id)
    {
        ServiceOrderId = serviceOrderId;
        _jobs = jobs;
        _parts = parts;
        _consumables = consumables;
        CreatedOn = nowUtc;

        TotalPriceCents =
            _jobs.Sum(x => x.PriceCents)
            + _parts.Sum(x => x.PriceCents * x.Quantity)
            + _consumables.Sum(x => x.PriceCents);
    }

    private Budget() : base(BudgetId.New())
    {
        ServiceOrderId = null!;
    }

    public static Budget Create(
        ServiceOrderId serviceOrderId,
        List<BudgetJob> jobs, List<BudgetPart> parts, List<BudgetConsumable> consumables, DateTime nowUtc) =>
        new(BudgetId.New(), serviceOrderId, jobs, parts, consumables, nowUtc);

    public Result MarkJobAsExecuted(Guid budgetJobId, DateTime nowUtc)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == budgetJobId);
        if (job is null)
            return Result.Failure(Error.NotFound("BudgetJob.NotFound", $"Serviço '{budgetJobId}' não encontrado no orçamento."));

        return job.MarkAsExecuted(nowUtc);
    }

    public Result Approve(DateTime nowUtc)
    {
        if (IsApproved.HasValue)
            return Result.Failure(Error.Conflict("Budget.AlreadyReviewed", "O orçamento já foi revisado."));

        IsApproved = true;
        ApprovedOn = nowUtc;
        return Result.Success();
    }

    public Result Reject()
    {
        if (IsApproved.HasValue)
            return Result.Failure(Error.Conflict("Budget.AlreadyReviewed", "O orçamento já foi revisado."));

        IsApproved = false;
        return Result.Success();
    }
}
