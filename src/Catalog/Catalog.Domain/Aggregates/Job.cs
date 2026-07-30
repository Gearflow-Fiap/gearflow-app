using Catalog.Domain.ValueObjects;
using Shared.Domain.Primitives;

namespace Catalog.Domain.Aggregates;

/// <summary>
/// Serviço de mão de obra ofertado pela oficina (o "Job"/"Serviço" do GearFlow). Referenciado pelos
/// itens de orçamento (<c>BudgetJob</c>) com o preço snapshotted no fechamento.
/// </summary>
public sealed class Job : AggregateRoot<JobId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    /// <summary>Preço em centavos (inteiro — nunca float/decimal para dinheiro).</summary>
    public int PriceCents { get; private set; }

    public DateTime CreatedOn { get; private set; }

    private Job(JobId id, string name, string description, int priceCents, DateTime createdOn)
        : base(id)
    {
        Name = name;
        Description = description;
        PriceCents = priceCents;
        CreatedOn = createdOn;
    }

    // ctor sem parâmetros para o EF materializar.
    private Job() : base(JobId.New())
    {
        Name = null!;
        Description = null!;
    }

    public static Result<Job> Create(string name, string description, int priceCents, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Job>(Error.Validation("Job.NameRequired", "Nome do serviço é obrigatório."));
        if (priceCents < 0)
            return Result.Failure<Job>(Error.Validation("Job.PriceNegative", "Preço não pode ser negativo."));

        return Result.Success(new Job(JobId.New(), name.Trim(), description?.Trim() ?? string.Empty, priceCents, nowUtc));
    }

    public Result Update(string name, string description, int priceCents)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(Error.Validation("Job.NameRequired", "Nome do serviço é obrigatório."));
        if (priceCents < 0)
            return Result.Failure(Error.Validation("Job.PriceNegative", "Preço não pode ser negativo."));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        PriceCents = priceCents;
        return Result.Success();
    }
}
