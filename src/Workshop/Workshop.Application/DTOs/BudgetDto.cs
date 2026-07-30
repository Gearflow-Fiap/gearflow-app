using Workshop.Domain.Aggregates.BudgetModel;

namespace Workshop.Application.DTOs;

public sealed record BudgetJobDto(Guid Id, Guid JobId, int PriceCents, bool IsExecuted, DateTime? ExecutedOn)
{
    public static BudgetJobDto FromEntity(BudgetJob j) => new(j.Id, j.JobId, j.PriceCents, j.IsExecuted, j.ExecutedOn);
}

public sealed record BudgetPartDto(Guid PartId, int PriceCents, int Quantity)
{
    public static BudgetPartDto FromEntity(BudgetPart p) => new(p.PartId, p.PriceCents, p.Quantity);
}

public sealed record BudgetConsumableDto(Guid ConsumableId, int PriceCents, decimal Quantity)
{
    public static BudgetConsumableDto FromEntity(BudgetConsumable c) => new(c.ConsumableId, c.PriceCents, c.Quantity);
}

public sealed record BudgetDto(
    Guid Id,
    Guid ServiceOrderId,
    int TotalPriceCents,
    bool? IsApproved,
    DateTime CreatedOn,
    DateTime? ApprovedOn,
    IReadOnlyList<BudgetJobDto> Jobs,
    IReadOnlyList<BudgetPartDto> Parts,
    IReadOnlyList<BudgetConsumableDto> Consumables)
{
    public static BudgetDto FromAggregate(Budget b) =>
        new(
            b.Id.Value,
            b.ServiceOrderId.Value,
            b.TotalPriceCents,
            b.IsApproved,
            b.CreatedOn,
            b.ApprovedOn,
            b.Jobs.Select(BudgetJobDto.FromEntity).ToList(),
            b.Parts.Select(BudgetPartDto.FromEntity).ToList(),
            b.Consumables.Select(BudgetConsumableDto.FromEntity).ToList());
}

/// <summary>Visão detalhada da OS (com o orçamento), equivalente ao GET /{id}/details do legado.</summary>
public sealed record ServiceOrderDetailsDto(ServiceOrderDto Order, BudgetDto? Budget);

/// <summary>Tempo médio (min) por fase — alimenta o dashboard da Fase 3.</summary>
public sealed record MonitoringAverageDto(
    double DiagnosticMinutes, double ExecutionMinutes, double FinalizationMinutes, int SampleSize);
